using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speakerName; // 说话者名字
    public string content; // 对话内容
    public Sprite speakerPortrait; // 说话者头像
    public AudioClip voiceClip; // 语音
    public List<DialogueOption> options; // 分支选项
    public bool endAfterThis; // 是否在显示后直接结束对话
}

[Serializable]
public class DialogueOption
{
    public string optionText; // 选项文本
    public int nextLineIndex; // 选择后跳转的对话行索引
    public bool isTriggerEvent; // 是否需要触发事件
    [NonSerialized] public int Index; // 用于动画延迟
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Config/DialogueData")]
public class DialogueData : ScriptableObject
{
    public int id; // id
    public List<DialogueLine> lines; // 对话行列表
    public List<int> taskIds;
    [Header("是否可跳过")] public bool canSkip = true;
    [Header("是否自动前进")] public bool autoAdvance;
    [Header("自动前进延迟")] public float autoAdvanceDelay = 3f;
}