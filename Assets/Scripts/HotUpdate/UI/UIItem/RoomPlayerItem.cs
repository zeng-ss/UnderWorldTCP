using HotUpdate.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    // 房间内玩家列表项：头像 + 昵称 + 准备 / 房主状态。
    public class RoomPlayerItem : MonoBehaviour
    {
        [SerializeField] private Image headImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;

        private static readonly Color ColMaster = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color ColReady = new Color(0.3f, 0.9f, 0.4f, 1f);
        private static readonly Color ColNotReady = new Color(0.62f, 0.64f, 0.7f, 1f);

        public void SetData(RoomPlayerData data, bool isSelf)
        {
            if (data == null) return;
            nameText.text = isSelf ? data.Nickname + "（我）" : data.Nickname;
            if (data.IsMaster)
            {
                stateText.text = "房主";
                stateText.color = ColMaster;
            }
            else
            {
                stateText.text = data.IsReady ? "已准备" : "未准备";
                stateText.color = data.IsReady ? ColReady : ColNotReady;
            }
        }
    }
}