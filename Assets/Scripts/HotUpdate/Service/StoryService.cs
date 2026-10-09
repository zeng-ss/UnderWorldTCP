using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;

namespace HotUpdate.Service
{
    /// <summary>
    /// 剧情服务：持有剧情数据源与进度，是「当前该播哪段对话」的唯一门面。
    /// </summary>
    public class StoryService
    {
        private List<DialogueData> _dialogues;

        // 当前剧情进度下标
        public int DialogueIndex { get; private set; }

        // 当前下标是否指向一段有效对话
        private bool HasDialogue =>
            _dialogues != null && _dialogues.Count > 0 && DialogueIndex >= 0 && DialogueIndex < _dialogues.Count;

        // 当前剧情对话
        public DialogueData Current => HasDialogue ? _dialogues[DialogueIndex] : null;

        // 是否已是最后一段剧情
        private bool IsLast => _dialogues == null || _dialogues.Count == 0 || DialogueIndex >= _dialogues.Count - 1;

        /// <summary>
        /// 注册剧情数据源
        /// </summary>
        public void Init()
        {
            for (int i = 1; i < 3; i++)
            {
                AppContext.Res.LoadAssetAsync<DialogueData>("DialogueData0" + i, asset =>
                {
                    _dialogues.Add(asset);
                });
            }
        }

        // 推进到下一段剧情 任务领奖时调用。已是最后一段则返回 false
        public bool Advance()
        {
            if (!HasDialogue || IsLast) return false;
            DialogueIndex++;
            NotifyChanged();
            return true;
        }

        /// <summary>重置到第一段剧情</summary>
        public void Reset()
        {
            DialogueIndex = 0;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            AppContext.Events.EventTrigger(GameEvent.StoryChanged);
        }
    }
}