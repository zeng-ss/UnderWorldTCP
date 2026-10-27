using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("BattleResultPanel")]
    public class BattleResultPanel : BasePanel
    {
        [SerializeField] private TMP_Text titleText, durationText, killText, rewardText;
        [SerializeField] private Image titleBar;
        [SerializeField] private Button backBtn;

        // 结算期间不让角色继续操作
        public override bool BlocksGameplayInput => true;

        private static readonly Color WinColor = new(0.85f, 1f, 0.24f, 1f);
        private static readonly Color LoseColor = new(1f, 0.42f, 0.38f, 1f);
        private bool _isWin;

        public override void Show()
        {
            base.Show();
            backBtn.onClick.AddListener(OnBackClicked);
        }

        public override void Hide()
        {
            base.Hide();
            backBtn.onClick.RemoveAllListeners();
        }

        public void Refresh(BattleResultNtf ntf)
        {
            _isWin = ntf.Result == 1;
            Color tint = _isWin ? WinColor : LoseColor;

            titleText.text = _isWin ? "战斗胜利" : "战斗失败";
            titleText.color = tint;
            titleBar.color = tint;

            int sec = Mathf.Max(0, ntf.DurationSec);
            durationText.text = $"{sec / 60:00}:{sec % 60:00}";
            killText.text = ntf.Kills.ToString();
            rewardText.text = string.IsNullOrEmpty(ntf.RewardDesc) ? "无" : ntf.RewardDesc;
        }

        private void OnBackClicked()
        {
            // 服务端会顺手把场景名重置回主界面，所以回大厅之后还能正常再开一局
            AppContext.Proto.RequestExitGameRoom(AppContext.Session.RoleId, _ =>
            {
                // 服务端已退房，客户端也要清掉本地房间状态，大厅才是房间列表形态
                AppContext.Ui.GetPanel<RoomPanel>()?.ResetRoomState();

                AppContext.Ui.ClosePanel<BattleResultPanel>();
                AppContext.Ui.ClosePanel<PlayerProPanel>();
                var type = _isWin ? TransitionPanel.ScrollBgType.Normal : TransitionPanel.ScrollBgType.Fail;
                AppContext.Scene.LoadScene("LobbyScene", 1f, type, () => AppContext.Ui.OpenPanel<RoomPanel>());
            });
        }
    }
}