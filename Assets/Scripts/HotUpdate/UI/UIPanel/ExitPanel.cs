using HotUpdate.Core;
using HotUpdate.Manager;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("ExitPanel")]
    public class ExitPanel : BasePanel
    {
        [FormerlySerializedAs("yesBtn")] public Button exitBtn;

        [FormerlySerializedAs("cancleBtn")] [FormerlySerializedAs("noBtn")]
        public Button cancelBtn;

        /// <summary>退出确认弹出时应锁住角色操作</summary>
        public override bool BlocksGameplayInput => true;

        private void OnEnable()
        {
            exitBtn.onClick.AddListener(Exit_OnClick);
            cancelBtn.onClick.AddListener(() => { AppContext.Ui.ClosePanel<ExitPanel>(); });
        }

        private void Exit_OnClick()
        {
            AppContext.Proto.RequestExitGameRoom(AppContext.Session.RoleId, ExitGameRoomCallback);
        }

        private void ExitGameRoomCallback(ExitGameRoomRet ret)
        {
            if (ret.CmdCode != CmdCode.Succeed)
            {
                if (!string.IsNullOrEmpty(ret.Tip)) AppContext.Ui.ShowTip(ret.Tip);
                return;
            }

            // 先收掉确认框再切场景：Canvas 是常驻的，面板不关会跟着回到主界面
            AppContext.Scene.LoadScene("EnterScene",0.7f, OnCoveredHandle);
        }

        private void OnCoveredHandle()
        {
            AppContext.Ui.ClosePanel(this);
            AppContext.Ui.ClosePanel<PlayerProPanel>();
            AppContext.Ui.OpenPanel<StartPanel>();
        }

        private void OnDisable()
        {
            exitBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.RemoveAllListeners();
        }
    }
}