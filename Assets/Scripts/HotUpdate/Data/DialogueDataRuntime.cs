using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Data
{
    // 对话的运行时模型。
    // 由 StoryService 从 Luban 的 dialogue / dialogueLine / dialogueOption 三张表组装而成，
    // 客户端不再有 DialogueData 这类 ScriptableObject 配置。
    // 立绘 / 语音在表里存的是资源地址（Addressable 地址），由展示层按需加载。

    // 一段对话
    public class DialogueDataRuntime
    {
        public readonly int Id;
        public readonly List<int> TaskIds = new();
        public readonly bool CanSkip;
        public readonly bool AutoAdvance;
        public readonly float AutoAdvanceDelay;
        public readonly List<DialogueLineRuntime> Lines = new();

        public DialogueDataRuntime(int id, bool canSkip, bool autoAdvance, float autoAdvanceDelay,
            IEnumerable<int> taskIds)
        {
            Id = id;
            CanSkip = canSkip;
            AutoAdvance = autoAdvance;
            AutoAdvanceDelay = autoAdvanceDelay;
            if (taskIds != null) TaskIds.AddRange(taskIds);
        }
    }

    // 对话中的一行
    public class DialogueLineRuntime
    {
        public readonly int Id;
        public readonly int LineIndex;
        public readonly string SpeakerName;
        public readonly string Content;
        public readonly string PortraitAddress; // 立绘资源地址，空表示不显示
        public readonly string VoiceAddress; // 语音资源地址，空表示无语音
        public readonly bool EndAfterThis;
        public readonly List<DialogueOptionRuntime> Options = new();

        public DialogueLineRuntime(int id, int lineIndex, string speakerName, string content,
            string portraitAddress, string voiceAddress, bool endAfterThis)
        {
            Id = id;
            LineIndex = lineIndex;
            SpeakerName = speakerName;
            Content = content;
            PortraitAddress = portraitAddress;
            VoiceAddress = voiceAddress;
            EndAfterThis = endAfterThis;
        }
    }

    // 对话的选项分支
    public class DialogueOptionRuntime
    {
        public readonly string OptionText;
        public readonly int NextLineIndex; // -1 表示结束对话
        public readonly bool IsTriggerEvent;

        [System.NonSerialized] public int Index; // 用于入场动画延迟

        public DialogueOptionRuntime(string optionText, int nextLineIndex, bool isTriggerEvent)
        {
            OptionText = optionText;
            NextLineIndex = nextLineIndex;
            IsTriggerEvent = isTriggerEvent;
        }
    }
}
