using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;

namespace HotUpdate.Service
{
    public class ChatService
    {
        /// <summary>本局累计的所有聊天消息</summary>
        public List<MessageData> Messages { get; } = new();

        /// <summary>
        /// 发送本地玩家的消息并广播。
        /// </summary>
        public void SendLocal(string content, string sendTime)
        {
            var message = new MessageData
            {
                SenderClientId = 0,
                Name = AppContext.Session.PlayerName,
                Message = content,
                SendTime = sendTime
            };

            Publish(message);
        }

        /// <summary>收到一条远端消息</summary>
        public void Receive(MessageData message)
        {
            if (message == null) return;
            Publish(message);
        }

        public void Clear()
        {
            Messages.Clear();
        }

        private void Publish(MessageData message)
        {
            Messages.Add(message);
            AppContext.Events.EventTrigger(GameEvent.ChatMessageReceived, new ChatMessageArgs(message));
        }
    }
}
