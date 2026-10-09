namespace HotUpdate.Data.Config
{
    // dialogueOption 配置表的一行，供 Json 反序列化使用。
    // 字段名必须与 Luban 导出的 tbdialogueoption.json 的键名完全一致（区分大小写）。
    public class DialogueOptionRow
    {
        public int id; // 选项编号
        public int lineId; // 所属对话行编号，对应 dialogueLine 表的 id
        public int optionIndex; // 在同一行内的顺序
        public string optionText; // 选项文本
        public int nextLineIndex; // 选择后跳转的行下标，-1 表示结束对话
        public bool isTriggerEvent; // 是否触发剧情收尾（解锁任务 / 生成敌人）
    }
}