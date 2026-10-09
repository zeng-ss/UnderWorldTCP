using DG.Tweening;
using HotUpdate.Controller;
using HotUpdate.Core;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("DialoguePanel")]
    public class DialoguePanel : BasePanel
    {
        #region 数据

        [Header("对话控制器")]
        public DialogueController dialogueController;

        [Header("左侧UI引用（玩家）")]
        public TMP_Text speakerNameTextL;
        public TMP_Text dialogueTextL;
        public Image speakerPortraitImageL;
        public Transform optionsPanelL;

        [Header("右侧UI引用（NPC）")]
        public TMP_Text speakerNameTextR;
        public TMP_Text dialogueTextR;
        public Image speakerPortraitImageR;
        public Transform optionsPanelR;

        [Header("公共UI")]
        public TMP_Text tipText; // 提示文字（居中显示）
        private Camera _followCamera;

        [Header("动画设置")]
        private float _fadeDuration = 0.3f;
        private float _textTypeDuration = 0.05f;
        private Ease _fadeEase = Ease.OutQuad;

        private Sequence _showSequence;
        private Tween _typingTween;
        private bool _isPlayerSpeaking = true;
        private float _lastSoundTime;

        #endregion

        protected override void Awake() { base.Awake(); _followCamera = Camera.main; }

        private void OnEnable()
        {
            // 面板显示动画
            transform.localScale = Vector3.zero;
            _showSequence = DOTween.Sequence().Append(transform.DOScale(Vector3.one, _fadeDuration).SetEase(_fadeEase)).Play();
        }

        /// <summary>
        /// 设置说话者并显示对应侧边
        /// </summary>
        public void SetSpeaker(string speakerName, Sprite portrait, bool isPlayer)
        {
            _isPlayerSpeaking = isPlayer;
            // 更新UI组件
            if (isPlayer)
            {
                speakerNameTextL.text = speakerName;
                speakerPortraitImageL.sprite = portrait;
                speakerPortraitImageL.gameObject.SetActive(true);
            }
            else
            {
                speakerNameTextR.text = speakerName;
                speakerPortraitImageR.sprite = portrait;
                speakerPortraitImageR.gameObject.SetActive(true);
            }
            // 通过控制器显示对应侧边
            if (dialogueController != null)
            {
                dialogueController.ShowSpeaker(isPlayer, () =>
                {
                    // 侧边显示完成后可以执行其他动画
                    PlaySpeakerEnterAnimation(isPlayer);
                });
            }
        }

        /// <summary>
        /// 播放说话者进入动画
        /// </summary>
        private void PlaySpeakerEnterAnimation(bool isPlayer)
        {
            // 名字动画
            TMP_Text nameText = isPlayer ? speakerNameTextL : speakerNameTextR;
            if (nameText != null)
            {
                nameText.transform.localScale = Vector3.zero;
                nameText.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack);
            }
            // 头像动画
            Image portraitImage = isPlayer ? speakerPortraitImageL : speakerPortraitImageR;
            if (portraitImage != null && portraitImage.gameObject.activeSelf)
            {
                portraitImage.transform.DOShakePosition(0.3f, 5f);
            }
        }

        /// <summary>
        /// 显示对话文本
        /// </summary>
        public void ShowDialogue(string content, AudioClip typingSound = null)
        {
            // 停止之前的打字效果
            _typingTween?.Kill();
            // 根据当前说话者选择对应的文本组件
            TMP_Text targetText = _isPlayerSpeaking ? dialogueTextL : dialogueTextR;
            if (targetText == null) return;
            // 重置文本
            targetText.text = "";
            targetText.alpha = 1;
            // 打字机效果
            int charCount = 0;
            _typingTween = DOTween.To(() => charCount, x => charCount = x, content.Length, content.Length * _textTypeDuration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    targetText.text = content.Substring(0, charCount);
                    // 打字音效
                    if (typingSound != null && charCount % 3 == 0)
                    {
                        float currentTime = Time.time;
                        if (currentTime - _lastSoundTime >= 0.1f)
                        {
                            AudioSource.PlayClipAtPoint(typingSound, _followCamera.transform.position, 0.05f);
                            _lastSoundTime = currentTime;
                        }
                    }
                })
                .OnComplete(() => { _typingTween = null; }).Play();
        }

        /// <summary>
        /// 立即完成打字
        /// </summary>
        public void CompleteCurrentTyping()
        {
            if (_typingTween != null && _typingTween.IsActive())
            {
                _typingTween.Complete();
                _typingTween.Kill();
                _typingTween = null;
            }
            // 显示提示文字
            ShowContinueHint();
        }

        /// <summary>
        /// 显示继续提示
        /// </summary>
        public void ShowContinueHint()
        {
            tipText.gameObject.SetActive(true);
            tipText.text = "点击空格继续.........";
        }

        /// <summary>
        /// 隐藏继续提示
        /// </summary>
        public void HideContinueHint()
        {
            if (tipText.gameObject.activeSelf) { tipText.gameObject.SetActive(false); }
        }

        /// <summary>
        /// 显示结束提示
        /// </summary>
        public void ShowEndHint()
        {
            tipText.gameObject.SetActive(true);
            tipText.text = "点击左键结束.........";
        }

        /// <summary>
        /// 获取当前选项面板
        /// </summary>
        public Transform GetCurrentOptionsPanel()
        {
            return _isPlayerSpeaking ? optionsPanelL : optionsPanelR;
        }

        /// <summary>
        /// 清空选项
        /// </summary>
        public void ClearOptions()
        {
            Transform currentPanel = GetCurrentOptionsPanel();
            if (currentPanel != null)
            {
                for (int i = currentPanel.childCount - 1; i >= 0; i--)
                {
                    Destroy(currentPanel.GetChild(i).gameObject);
                }
            }
        }

        /// <summary>
        /// 检查是否有选项
        /// </summary>
        public bool HasOptions()
        {
            Transform currentPanel = GetCurrentOptionsPanel();
            return currentPanel != null && currentPanel.childCount > 0;
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void ClosePanel()
        {
            // 停止所有动画
            _showSequence?.Kill();
            _typingTween?.Kill();
            Sequence closeSequence = DOTween.Sequence();
            // 隐藏所有侧边
            if (dialogueController != null)
            {
                closeSequence.AppendCallback(() => dialogueController.HideAllSides());
            }
            // 面板缩放消失
            closeSequence.Append(transform.DOScale(Vector3.zero, _fadeDuration)
                    .SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    AppContext.Ui.ClosePanel<DialoguePanel>();
                })
                .Play();
        }
    }
}
