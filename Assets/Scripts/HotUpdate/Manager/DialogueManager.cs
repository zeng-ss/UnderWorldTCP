using System.Collections;
using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.UI.UIItem;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Manager
{
    public class DialogueManager : MonoBehaviour
    {
        private void Awake()
        {
            RegisterDialogueInput();
        }

        // 当前对话状态
        private bool _isTyping;
        private int _currentLineIndex;
        private bool _isWaitingClickForEnd;
        private DialogueData _currentDialogue;

        private DialoguePanel _currentPanel;
        private Queue<DialogueLine> _dialogueQueue = new();

        // 协程引用管理
        private Coroutine _typingCoroutine;
        private Coroutine _waitForClickCoroutine;

        // 输入冷却
        private float _lastSpacePressTime;
        private const float SpaceCooldown = 0.6f;
        private const float TypingSpeed = 0.05f; // 每个字符的显示时间

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(DialogueData dialogueData)
        {
            if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Count == 0)
            {
                Debug.LogWarning("对话数据为空或无效！");
                return;
            }

            // 如果已经有对话在进行，先结束
            if (IsDialogueActive())
            {
                EndDialogue();
            }

            _currentDialogue = dialogueData;
            _currentLineIndex = 0;
            _dialogueQueue.Clear();
            // 将所有对话行加入队列
            foreach (var line in dialogueData.lines)
            {
                _dialogueQueue.Enqueue(line);
            }

            // 打开对话面板
            AppContext.Ui.OpenPanel<DialoguePanel>(panel =>
            {
                _currentPanel = panel;
                if (_currentPanel == null)
                {
                    Debug.LogError("无法打开对话面板！");
                    return;
                }

                // 显示第一句对话
                DisplayNextLine();
            });
        }

        /// <summary>
        /// 显示下一句对话
        /// </summary>
        public void DisplayNextLine()
        {
            // 安全检查
            if (_currentPanel == null)
            {
                EndDialogue();
                return;
            }

            // 如果正在打字，完成当前打字效果
            if (_isTyping)
            {
                CompleteTyping();
                return;
            }

            // 清理UI状态
            _currentPanel.ClearOptions();
            _currentPanel.HideContinueHint();
            CancelInvoke(nameof(DisplayNextLine));

            // 检查队列是否为空
            if (_dialogueQueue.Count == 0)
            {
                EndDialogue();
                return;
            }

            DialogueLine line = _dialogueQueue.Dequeue();
            _currentLineIndex++;
            // 判断说话者类型
            bool isPlayer = line.speakerName == "玩家";
            // 设置说话者和对应侧边
            _currentPanel.SetSpeaker(line.speakerName, line.speakerPortrait, isPlayer);
            // 开始打字机效果
            _isTyping = true;
            _currentPanel.ShowDialogue(line.content, line.voiceClip);
            // 启动打字完成协程
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }

            _typingCoroutine = StartCoroutine(WaitForTypingComplete(line));
        }

        /// <summary>
        /// 完成当前打字效果
        /// </summary>
        private void CompleteTyping()
        {
            if (_currentPanel != null)
            {
                _currentPanel.CompleteCurrentTyping();
            }

            DialogueLine line = _currentDialogue.lines[_currentLineIndex - 1];
            _isTyping = false;
            // 停止打字协程
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            // 处理选项
            if (line.options != null && line.options.Count > 0)
            {
                ShowOptions(line.options);
            }
            else
            {
                // 处理对话结束逻辑
                if (line.endAfterThis)
                {
                    HandleDialogueEnd();
                }
                else
                {
                    // 显示继续提示
                    if (_currentPanel == null || !_currentPanel.gameObject.activeInHierarchy) return;
                    _currentPanel.ShowContinueHint();
                    // 自动前进
                    if (_currentDialogue != null && _currentDialogue.autoAdvance)
                    {
                        Invoke(nameof(DisplayNextLine), _currentDialogue.autoAdvanceDelay);
                    }
                }
            }
        }

        /// <summary>
        /// 等待打字完成
        /// </summary>
        private IEnumerator WaitForTypingComplete(DialogueLine line)
        {
            if (line == null || string.IsNullOrEmpty(line.content))
            {
                _isTyping = false;
                yield break;
            }

            // 等待打字完成
            float typingDuration = line.content.Length * TypingSpeed;
            yield return new WaitForSeconds(typingDuration);
            _isTyping = false;
            _typingCoroutine = null;
            // 处理选项
            if (line.options != null && line.options.Count > 0)
            {
                ShowOptions(line.options);
            }
            else
            {
                // 处理对话结束逻辑
                if (line.endAfterThis)
                {
                    HandleDialogueEnd();
                }
                else
                {
                    // 显示继续提示
                    if (_currentPanel == null || !_currentPanel.gameObject.activeInHierarchy) yield break;
                    _currentPanel.ShowContinueHint();
                    // 自动前进
                    if (_currentDialogue != null && _currentDialogue.autoAdvance)
                    {
                        Invoke(nameof(DisplayNextLine), _currentDialogue.autoAdvanceDelay);
                    }
                }
            }
        }

        /// <summary>
        /// 处理对话结束（等待点击）
        /// </summary>
        private void HandleDialogueEnd()
        {
            if (_currentPanel == null) return;
            _currentPanel.ShowEndHint();
            _isWaitingClickForEnd = true;
            // 停止之前的等待协程
            if (_waitForClickCoroutine != null)
            {
                StopCoroutine(_waitForClickCoroutine);
            }

            _waitForClickCoroutine = StartCoroutine(WaitForEndClick());
        }

        /// <summary>
        /// 等待结束点击
        /// </summary>
        private IEnumerator WaitForEndClick()
        {
            while (_isWaitingClickForEnd)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isWaitingClickForEnd = false;
                    _waitForClickCoroutine = null;
                    // 触发事件
                    AppContext.Events.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(_currentDialogue.id));
                    EndDialogue();
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 显示选项
        /// </summary>
        private void ShowOptions(List<DialogueOption> options)
        {
            if (_currentPanel == null || options == null || options.Count == 0)
            {
                return;
            }

            // 获取当前说话者的选项面板
            Transform optionsPanel = _currentPanel.GetCurrentOptionsPanel();
            if (optionsPanel == null)
            {
                Debug.LogError("无法获取选项面板！");
                return;
            }

            // 清空现有选项
            _currentPanel.ClearOptions();
            // 加载并创建选项按钮
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                option.Index = i;
                AppContext.Res.LoadAndInstantiateAsync("DialogueOptionItem", optionsPanel,
                    optionObj =>
                    {
                        if (optionObj == null)
                        {
                            Debug.LogError($"选项预制体加载失败！索引: {i}");
                            return;
                        }

                        var optionItem = optionObj.GetComponent<DialogueOptionItem>();
                        if (optionItem != null)
                        {
                            optionItem.SetupOption(option);
                        }
                        else
                        {
                            Debug.LogError($"DialogueOptionItem 组件未找到！索引: {i}");
                        }
                    });
            }
        }

        /// <summary>
        /// 选项被选择
        /// </summary>
        public void OnOptionSelected(DialogueOption option)
        {
            if (option == null)
            {
                Debug.LogError("选项为空！");
                return;
            }

            // 清空当前队列
            _dialogueQueue.Clear();
            // 检查特殊值：-1 表示结束对话
            if (option.nextLineIndex == -1)
            {
                if (option.isTriggerEvent)
                {
                    // 触发事件
                    AppContext.Events.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(_currentDialogue.id));
                }

                EndDialogue();
                return;
            }

            // 跳转到指定行
            if (_currentDialogue != null && option.nextLineIndex >= 0 &&
                option.nextLineIndex < _currentDialogue.lines.Count)
            {
                // 从指定行开始重新填充队列
                for (int i = option.nextLineIndex; i < _currentDialogue.lines.Count; i++)
                {
                    _dialogueQueue.Enqueue(_currentDialogue.lines[i]);
                }

                _currentLineIndex = option.nextLineIndex;
                _isTyping = false;
                // 立即显示下一句
                CancelInvoke(nameof(DisplayNextLine));
                DisplayNextLine();
            }
            else
            {
                Debug.LogWarning($"无效的对话行索引: {option.nextLineIndex}");
                EndDialogue();
            }
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        private void EndDialogue()
        {
            // 停止所有协程
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            if (_waitForClickCoroutine != null)
            {
                StopCoroutine(_waitForClickCoroutine);
                _waitForClickCoroutine = null;
            }

            // 取消所有 Invoke
            CancelInvoke();
            // 关闭面板
            if (_currentPanel != null)
            {
                _currentPanel.ClosePanel();
                _currentPanel = null;
            }

            // 重置状态
            _currentDialogue = null;
            _currentLineIndex = 0;
            _dialogueQueue.Clear();
            _isTyping = false;
            _isWaitingClickForEnd = false;
            AppContext.Events.EventTrigger(GameEvent.CursorHide);
        }

        /// <summary>
        /// 判断是否正在对话
        /// </summary>
        private bool IsDialogueActive()
        {
            return _currentPanel != null && _currentPanel.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 跳过当前对话
        /// </summary>
        private void SkipCurrentDialogue()
        {
            if (_currentDialogue != null && _currentDialogue.canSkip)
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// 对话期间的按键。注册到 InputManager，不再自己开 Update 轮询。
        /// </summary>
        private void RegisterDialogueInput()
        {
            InputManager.Instance.RegisterKeyDown(KeyCode.Space, OnSpacePressed);
            InputManager.Instance.RegisterKeyDown(KeyCode.Escape, OnEscapePressed);
        }

        private void UnregisterDialogueInput()
        {
            InputManager.Instance.UnregisterKeyDown(KeyCode.Space, OnSpacePressed);
            InputManager.Instance.UnregisterKeyDown(KeyCode.Escape, OnEscapePressed);
        }

        private void OnSpacePressed()
        {
            if (!IsDialogueActive()) return;
            if (_isWaitingClickForEnd || _currentPanel.HasOptions()) return;
            if (Time.time - _lastSpacePressTime < SpaceCooldown) return;

            _lastSpacePressTime = Time.time;
            DisplayNextLine();
        }

        private void OnEscapePressed()
        {
            if (!IsDialogueActive()) return;
            if (_isWaitingClickForEnd || _currentPanel.HasOptions()) return;
            SkipCurrentDialogue();
        }

        /// <summary>
        /// 清理资源（当对象被销毁时）
        /// </summary>
        protected void OnDestroy()
        {
            UnregisterDialogueInput();
            EndDialogue();
        }
    }
}
