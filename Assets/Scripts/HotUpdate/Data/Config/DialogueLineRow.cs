namespace HotUpdate.Data.Config
{
    // dialogueLine 配置表的一行，供 Json 反序列化使用。
    // 字段名必须与 Luban 导出的 tbdialogueline.json 的键名完全一致（区分大小写）。
    public class DialogueLineRow
    {
        public int id; // 行编号
        public int dialogueId; // 所属对话编号，对应 dialogue 表的 id
        public int lineIndex; // 在所属对话内的顺序
        public string speakerName; // 说话者名字
        public string content; // 对话内容
        public string portrait; // 立绘的 Addressable 地址，空表示不显示
        public string voice; // 语音的 Addressable 地址，空表示无语音
        public bool endAfterThis; // 本行结束后是否直接进入结束等待
    }
}