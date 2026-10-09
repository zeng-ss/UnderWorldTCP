using System.Collections.Generic;

namespace HotUpdate.Data.Config
{
    // dialogue 配置表的一行，供 Json 反序列化使用。
    // 字段名必须与 Luban 导出的 tbdialogue.json 的键名完全一致（区分大小写）。
    public class DialogueRow
    {
        public int id; // 对话编号，也是播放顺序
        public List<int> taskIds; // 本段对话结束后解锁的任务编号
        public bool canSkip; // 是否允许 Esc 跳过
        public bool autoAdvance; // 是否自动前进
        public float autoAdvanceDelay; // 自动前进延迟（秒）
    }
}