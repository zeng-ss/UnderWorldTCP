using HotUpdate.Data;
using TMPro;
using UnityEngine;

namespace HotUpdate.UI.UIItem
{
    // 房间列表项（房间大厅里的一行）。
    // 展示脚本只负责填数据；点击回调由 RoomPanel 统一绑定（item 会被 LoopList 复用）。
    public class RoomListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text infoText;

        // 当前这项对应的房间 id，供面板点击时取用
        private int _roomId;
        public int RoomId => _roomId;

        public void SetData(RoomItemData data)
        {
            if (data == null) return;
            _roomId = data.RoomId;
            nameText.text = string.IsNullOrEmpty(data.RoomName) ? $"房间 {data.RoomId}" : data.RoomName;
            string state = data.IsPlaying ? "游戏中" : "等待中";
            infoText.text = $"房主：{data.MasterNickname}    {data.PlayerCount}/{data.MaxPlayer}    {state}";
        }
    }
}