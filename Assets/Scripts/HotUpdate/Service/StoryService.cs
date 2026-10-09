using System.Collections.Generic;
using HotUpdate.Data;
using HotUpdate.Data.Config;

namespace HotUpdate.Service
{
    /// <summary>
    /// 剧情服务：持有剧情数据源与进度，是「当前该播哪段对话」的唯一门面。
    /// 对话内容来自 Luban 导出的 tbdialogue / tbdialogueline / tbdialogueoption 三张 Json 表
    /// （与服务端同一份 Excel），客户端不再有 DialogueData 这类 ScriptableObject 配置。
    /// </summary>
    public class StoryService
    {
        private readonly Dictionary<int, DialogueDataRuntime> _byId = new();
        private readonly List<DialogueDataRuntime> _dialogues = new();

        // 当前剧情进度下标
        public int DialogueIndex { get; private set; }

        // 当前下标是否指向一段有效对话
        private bool HasDialogue =>
            DialogueIndex >= 0 && DialogueIndex < _dialogues.Count;

        // 当前剧情对话
        public DialogueDataRuntime Current => HasDialogue ? _dialogues[DialogueIndex] : null;

        // 是否已是最后一段剧情
        private bool IsLast => _dialogues.Count == 0 || DialogueIndex >= _dialogues.Count - 1;

        /// <summary>
        /// 用三张配置表重建剧情数据（按 id 升序播放），并复位进度。
        /// 只在进游戏时调用一次；后续配置变化走热更重新加载。
        /// </summary>
        public void Init(IReadOnlyList<DialogueRow> dialogues, IReadOnlyList<DialogueLineRow> lines,
            IReadOnlyList<DialogueOptionRow> options)
        {
            _byId.Clear();
            _dialogues.Clear();
            DialogueIndex = 0;

            if (dialogues == null) return;

            // 行按 dialogueId 分组、按 lineIndex 排序
            var linesByDialogue = new Dictionary<int, List<DialogueLineRow>>();
            if (lines != null)
            {
                foreach (DialogueLineRow line in lines)
                {
                    if (line == null) continue;
                    if (!linesByDialogue.TryGetValue(line.dialogueId, out var list))
                    {
                        list = new List<DialogueLineRow>();
                        linesByDialogue[line.dialogueId] = list;
                    }

                    list.Add(line);
                }
            }

            // 选项按 lineId 分组、按 optionIndex 排序
            var optionsByLine = new Dictionary<int, List<DialogueOptionRow>>();
            if (options != null)
            {
                foreach (DialogueOptionRow option in options)
                {
                    if (option == null) continue;
                    if (!optionsByLine.TryGetValue(option.lineId, out var list))
                    {
                        list = new List<DialogueOptionRow>();
                        optionsByLine[option.lineId] = list;
                    }

                    list.Add(option);
                }
            }

            foreach (DialogueRow row in dialogues)
            {
                if (row == null) continue;

                var runtime = new DialogueDataRuntime(row.id, row.canSkip, row.autoAdvance,
                    row.autoAdvanceDelay, row.taskIds);

                if (linesByDialogue.TryGetValue(row.id, out var linesOfDialogue))
                {
                    linesOfDialogue.Sort((a, b) => a.lineIndex.CompareTo(b.lineIndex));
                    foreach (DialogueLineRow line in linesOfDialogue)
                    {
                        var lineRuntime = new DialogueLineRuntime(line.id, line.lineIndex, line.speakerName,
                            line.content, line.portrait, line.voice, line.endAfterThis);

                        if (optionsByLine.TryGetValue(line.id, out var optionsOfLine))
                        {
                            optionsOfLine.Sort((a, b) => a.optionIndex.CompareTo(b.optionIndex));
                            foreach (DialogueOptionRow option in optionsOfLine)
                            {
                                lineRuntime.Options.Add(new DialogueOptionRuntime(option.optionText,
                                    option.nextLineIndex, option.isTriggerEvent));
                            }
                        }

                        runtime.Lines.Add(lineRuntime);
                    }
                }

                _byId[runtime.Id] = runtime;
                _dialogues.Add(runtime);
            }

            // 播放顺序按对话 id 升序（策划在 Excel 里按 id 排即可）
            _dialogues.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        /// <summary>按 id 取一段对话，不存在返回 null</summary>
        public DialogueDataRuntime GetById(int dialogueId) => _byId.GetValueOrDefault(dialogueId);

        // 推进到下一段剧情，任务领奖时调用。已是最后一段则返回 false
        public bool Advance()
        {
            if (!HasDialogue || IsLast) return false;
            DialogueIndex++;
            return true;
        }

        /// <summary>重置到第一段剧情</summary>
        public void Reset()
        {
            DialogueIndex = 0;
        }
    }
}
