using DG.Tweening;
using HotUpdate.Core;
using HotUpdate.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("ExitPanel")]
    public class ExitPanel : BasePanel
    {
        public Button yesBtn;
        public Button noBtn;
        private Vector3 _startPos = new(0, 840, 0);

        /// <summary>退出确认弹出时应锁住角色操作</summary>
        public override bool BlocksGameplayInput => true;

        private void OnEnable()
        {
            transform.localPosition = _startPos;
            transform.DOLocalMove(Vector3.zero, 0.5f);
            yesBtn.onClick.AddListener(() =>
            {
                //GameManager.Instance.SaveArchive();
                Application.Quit();
            });
            noBtn.onClick.AddListener(() =>
            {
                transform.DOLocalMove(_startPos, 0.5f)
                    .OnComplete(() => { AppContext.Ui.ClosePanel<ExitPanel>(); });
            });
        }

        private void OnDisable()
        {
            yesBtn.onClick.RemoveAllListeners();
            noBtn.onClick.RemoveAllListeners();
        }
    }
}
