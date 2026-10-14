using HotUpdate.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    // 聊天气泡项（ChatRootLeft / ChatRootRight 预制体上挂的就是这个）。
    // 气泡高度由预制体里的嵌套 LayoutGroup 自适应，代码只负责灌文本。
    public class ChatItem : MonoBehaviour
    {
        public Image headImage;
        public TMP_Text nameText;
        public TMP_Text mesText;

        /// <summary>按「名称 / 时间 / 内容」填充气泡（好友私聊等非 MessageData 场景直接用它）</summary>
        public void SetContent(string name, string time, string message)
        {
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(time) ? name : $"{name}——{time}";

            if (mesText != null)
                mesText.text = message;
        }

        /// <summary>用通用聊天数据结构填充气泡</summary>
        public void UpdateDate(MessageData data)
        {
            if (data == null) return;
            SetContent(data.Name, data.SendTime, data.Message);
        }
    }
}
