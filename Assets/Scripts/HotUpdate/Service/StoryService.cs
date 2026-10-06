/// <summary>
/// 剧情进度服务。原本是 GameManager.dialogueId —— 决定当前该播哪段对话，
/// 任务完成时推进。独立出来的原因是它既不属于任务数据也不属于 UI。
/// </summary>
public class StoryService
{
    /// <summary>当前剧情对话索引。NPCCtrl 用它选 dialogueDatas 里的对话。</summary>
    public int DialogueIndex { get; private set; }

    public void SetIndex(int index)
    {
        DialogueIndex = index < 0 ? 0 : index;
    }

    /// <summary>推进到下一段剧情（任务完成时调用）</summary>
    public void Advance()
    {
        DialogueIndex++;
    }

    public void Reset()
    {
        DialogueIndex = 0;
    }
}