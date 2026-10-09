using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.UI.UIItem;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Manager
{
    /// <summary>
    /// 对话流程管理器
    /// 对话数据来自 <c>StoryService.Current</c>，本类不再持有对话内容；
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        /// <summary>对话流程状态</summary>
        private enum DialogueState
        {
            Idle, // 无对话进行
            Typing, // 打字机正在播放
            WaitingAdvance, // 打字完成，等待玩家按键继续
            ShowingOptions, // 正在展示分支选项，等待选择
            WaitingEnd // 已到对话末尾，等待左键结束
        }

        private const float SpaceCooldown = 0.6f;
        private const string PlayerSpeakerName = "玩家";

        private DialogueState _state = DialogueState.Idle;

        private DialogueData _dialogue; // 当前对话数据
        private DialogueLine _line; // 当前正在展示的行
        private int _lineIndex = -1; // 当前行下标（-1 表示尚未开始）
        private DialoguePanel _panel;

        private float _autoAdvanceTimer; // 自动前进计时
        private float _lastSpaceTime; // 空格按下的冷却

        #region 生命周期

        private void Awake()
        {
            InputManager.Instance.RegisterKeyDown(KeyCode.Space, OnSpacePressed);
            InputManager.Instance.RegisterKeyDown(KeyCode.Escape, OnEscapePressed);
            InputManager.Instance.RegisterMouseDown(0, OnLeftClicked);
        }

        private void Update()
        {
            // 自动前进：只在「等待继续」状态下计时
            if (_state != DialogueState.WaitingAdvance) return;
            if (_dialogue == null || !_dialogue.autoAdvance) return;

            _autoAdvanceTimer += Time.deltaTime;
            if (_autoAdvanceTimer >= _dialogue.autoAdvanceDelay) AdvanceLine();
        }

        private void OnDestroy()
        {
            InputManager.Instance.UnregisterKeyDown(KeyCode.Space, OnSpacePressed);
            InputManager.Instance.UnregisterKeyDown(KeyCode.Escape, OnEscapePressed);
            InputManager.Instance.UnregisterMouseDown(0, OnLeftClicked);
            End(fireDialogueEnd: false);
        }

        #endregion

        #region 对外入口

        /// <summary>开始播放当前剧情（数据取自 StoryService，不再由外部传入）</summary>
        public void StartDialogue()
        {
            DialogueData data = AppContext.Story.Current;
            if (data == null || data.lines == null || data.lines.Count == 0)
            {
                Debug.LogWarning("DialogueManager: 当前没有可播放的剧情对话");
                return;
            }

            // 已有对话在进行：先静默收尾，避免状态残留
            if (_state != DialogueState.Idle) End(fireDialogueEnd: false);

            _dialogue = data;
            _line = null;
            _lineIndex = -1;
            AppContext.Events.EventTrigger(GameEvent.CursorShow);

            AppContext.Ui.OpenPanel<DialoguePanel>(panel =>
            {
                _panel = panel;
                if (_panel == null)
                {
                    Debug.LogError("DialogueManager: 对话面板打开失败");
                    End(fireDialogueEnd: false);
                    return;
                }

                _panel.OnTypingComplete += HandleTypingComplete;
                AdvanceLine();
            });
        }

        /// <summary>选项被选择（由 DialogueOptionItem 回调）</summary>
        public void OnOptionSelected(DialogueOption option)
        {
            if (option == null || _state != DialogueState.ShowingOptions) return;

            // -1 表示结束对话
            if (option.nextLineIndex == -1)
            {
                End(fireDialogueEnd: option.isTriggerEvent);
                return;
            }

            // 跳转到指定行（AdvanceLine 会自增到该行）
            if (_dialogue != null && option.nextLineIndex >= 0 && option.nextLineIndex < _dialogue.lines.Count)
            {
                _panel?.ClearOptions();
                _lineIndex = option.nextLineIndex - 1;
                AdvanceLine();
            }
            else
            {
                Debug.LogWarning($"DialogueManager: 无效的对话行索引 {option.nextLineIndex}");
                End(fireDialogueEnd: false);
            }
        }

        #endregion

        #region 状态机

        // 推进到下一行；无更多行则结束
        private void AdvanceLine()
        {
            if (_dialogue == null)
            {
                End(fireDialogueEnd: false);
                return;
            }

            // 跳过空行
            _line = null;
            while (++_lineIndex < _dialogue.lines.Count)
            {
                if (_dialogue.lines[_lineIndex] != null)
                {
                    _line = _dialogue.lines[_lineIndex];
                    break;
                }
            }

            if (_line == null)
            {
                End(fireDialogueEnd: false);
                return;
            }

            bool isPlayer = _line.speakerName == PlayerSpeakerName;
            if (_panel != null)
            {
                _panel.ClearOptions();
                _panel.HideContinueHint();
                _panel.SetSpeaker(_line.speakerName, _line.speakerPortrait, isPlayer);
            }

            _state = DialogueState.Typing;
            _panel?.ShowDialogue(_line.content, _line.voiceClip); // 完成后回调 HandleTypingComplete
        }

        // 打字完成（自然结束或玩家跳过）后的分支
        private void HandleTypingComplete()
        {
            if (_state != DialogueState.Typing || _line == null) return;

            // 有选项 → 展示选项
            if (_line.options is { Count: > 0 })
            {
                _state = DialogueState.ShowingOptions;
                ShowOptions(_line.options);
                return;
            }

            // 本行后结束 → 等待左键结束
            if (_line.endAfterThis)
            {
                _state = DialogueState.WaitingEnd;
                _panel?.ShowEndHint();
                return;
            }

            // 否则等待继续（可能是自动前进）
            _state = DialogueState.WaitingAdvance;
            _autoAdvanceTimer = 0f;
            _panel?.ShowContinueHint();
        }

        private void ShowOptions(List<DialogueOption> options)
        {
            Transform parent = _panel != null ? _panel.GetCurrentOptionsPanel() : null;
            if (parent == null)
            {
                Debug.LogError("DialogueManager: 无法获取选项面板");
                End(fireDialogueEnd: false);
                return;
            }

            _panel.ClearOptions();
            for (int i = 0; i < options.Count; i++)
            {
                DialogueOption option = options[i];
                option.Index = i; // 用于入场动画延迟
                AppContext.Res.LoadAndInstantiateAsync("DialogueOptionItem", parent, obj =>
                {
                    DialogueOptionItem item = obj?.GetComponent<DialogueOptionItem>();
                    if (item is not null) item.SetupOption(option, this);
                    else Debug.LogError($"DialogueManager: DialogueOptionItem 加载/组件缺失，索引 {option.Index}");
                });
            }
        }

        /// <summary>结束对话并收尾。fireDialogueEnd 为 true 时广播 DialogueEnd（携带刚结束的对话数据）。</summary>
        private void End(bool fireDialogueEnd)
        {
            DialogueData ended = _dialogue;

            if (fireDialogueEnd && ended != null)
            {
                AppContext.Events.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(ended));
            }

            _state = DialogueState.Idle;
            _line = null;
            _lineIndex = -1;
            _autoAdvanceTimer = 0f;
            _dialogue = null;

            if (_panel != null)
            {
                _panel.OnTypingComplete -= HandleTypingComplete;
                _panel.ClosePanel();
                _panel = null;
            }

            AppContext.Events.EventTrigger(GameEvent.CursorHide);
        }

        #endregion

        #region 输入

        private void OnSpacePressed()
        {
            if (_state != DialogueState.Typing && _state != DialogueState.WaitingAdvance) return;
            if (Time.time - _lastSpaceTime < SpaceCooldown) return;
            _lastSpaceTime = Time.time;

            if (_state == DialogueState.Typing) _panel?.CompleteTyping(); // 跳过打字 → 触发 HandleTypingComplete
            else AdvanceLine();
        }

        private void OnEscapePressed()
        {
            if (_state == DialogueState.Idle) return;
            if (_dialogue != null && _dialogue.canSkip) End(fireDialogueEnd: false);
        }

        private void OnLeftClicked()
        {
            if (_state != DialogueState.WaitingEnd) return;
            End(fireDialogueEnd: true);
        }

        #endregion
    }
}