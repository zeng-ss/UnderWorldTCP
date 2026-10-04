using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : UnitySingleTonMono<DialogueManager>
{
    // 当前对话状态
    private bool isTyping;
    private int currentLineIndex;
    private bool isWaitingClickForEnd;
    private DialogueData currentDialogue;

    private DialoguePanel currentPanel;
    private Queue<DialogueLine> dialogueQueue = new();

    // 协程引用管理
    private Coroutine typingCoroutine;
    private Coroutine waitForClickCoroutine;

    // 输入冷却
    private float lastSpacePressTime;
    private const float SPACE_COOLDOWN = 0.6f;
    private const float TYPING_SPEED = 0.05f; // 每个字符的显示时间

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Count == 0)
        {
            Debug.LogWarning("对话数据为空或无效！");
            return;
        }

        // 如果已经有对话在进行，先结束
        if (IsDialogueActive())
        {
            EndDialogue();
        }

        currentDialogue = dialogueData;
        currentLineIndex = 0;
        dialogueQueue.Clear();
        // 将所有对话行加入队列
        foreach (var line in dialogueData.lines)
        {
            dialogueQueue.Enqueue(line);
        }

        // 打开对话面板
        UIManager.Instance.OpenPanel<DialoguePanel>(panel =>
        {
            currentPanel = panel;
            if (currentPanel == null)
            {
                Debug.LogError("无法打开对话面板！");
                return;
            }

            // 显示第一句对话
            DisplayNextLine();
        });
    }

    /// <summary>
    /// 显示下一句对话
    /// </summary>
    public void DisplayNextLine()
    {
        // 安全检查
        if (currentPanel == null)
        {
            EndDialogue();
            return;
        }

        // 如果正在打字，完成当前打字效果
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        // 清理UI状态
        currentPanel.ClearOptions();
        currentPanel.HideContinueHint();
        CancelInvoke(nameof(DisplayNextLine));

        // 检查队列是否为空
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();
        currentLineIndex++;
        // 判断说话者类型
        bool isPlayer = line.speakerName == "玩家";
        // 设置说话者和对应侧边
        currentPanel.SetSpeaker(line.speakerName, line.speakerPortrait, isPlayer);
        // 开始打字机效果
        isTyping = true;
        currentPanel.ShowDialogue(line.content, line.voiceClip);
        // 启动打字完成协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(WaitForTypingComplete(line));
    }

    /// <summary>
    /// 完成当前打字效果
    /// </summary>
    private void CompleteTyping()
    {
        if (currentPanel != null)
        {
            currentPanel.CompleteCurrentTyping();
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex - 1];
        isTyping = false;
        // 停止打字协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 处理选项
        if (line.options != null && line.options.Count > 0)
        {
            ShowOptions(line.options);
        }
        else
        {
            // 处理对话结束逻辑
            if (line.endAfterThis)
            {
                HandleDialogueEnd();
            }
            else
            {
                // 显示继续提示
                if (currentPanel == null || !currentPanel.gameObject.activeInHierarchy) return;
                currentPanel.ShowContinueHint();
                // 自动前进
                if (currentDialogue != null && currentDialogue.autoAdvance)
                {
                    Invoke(nameof(DisplayNextLine), currentDialogue.autoAdvanceDelay);
                }
            }
        }
    }

    /// <summary>
    /// 等待打字完成
    /// </summary>
    private IEnumerator WaitForTypingComplete(DialogueLine line)
    {
        if (line == null || string.IsNullOrEmpty(line.content))
        {
            isTyping = false;
            yield break;
        }

        // 等待打字完成
        float typingDuration = line.content.Length * TYPING_SPEED;
        yield return new WaitForSeconds(typingDuration);
        isTyping = false;
        typingCoroutine = null;
        // 处理选项
        if (line.options != null && line.options.Count > 0)
        {
            ShowOptions(line.options);
        }
        else
        {
            // 处理对话结束逻辑
            if (line.endAfterThis)
            {
                HandleDialogueEnd();
            }
            else
            {
                // 显示继续提示
                if (currentPanel == null || !currentPanel.gameObject.activeInHierarchy) yield break;
                currentPanel.ShowContinueHint();
                // 自动前进
                if (currentDialogue != null && currentDialogue.autoAdvance)
                {
                    Invoke(nameof(DisplayNextLine), currentDialogue.autoAdvanceDelay);
                }
            }
        }
    }

    /// <summary>
    /// 处理对话结束（等待点击）
    /// </summary>
    private void HandleDialogueEnd()
    {
        if (currentPanel == null) return;
        currentPanel.ShowEndHint();
        isWaitingClickForEnd = true;
        // 停止之前的等待协程
        if (waitForClickCoroutine != null)
        {
            StopCoroutine(waitForClickCoroutine);
        }

        waitForClickCoroutine = StartCoroutine(WaitForEndClick());
    }

    /// <summary>
    /// 等待结束点击
    /// </summary>
    private IEnumerator WaitForEndClick()
    {
        while (isWaitingClickForEnd)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isWaitingClickForEnd = false;
                waitForClickCoroutine = null;
                // 触发事件
                EventMgr.Instance.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(currentDialogue.id));
                EndDialogue();
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 显示选项
    /// </summary>
    private void ShowOptions(List<DialogueOption> options)
    {
        if (currentPanel == null || options == null || options.Count == 0)
        {
            return;
        }

        // 获取当前说话者的选项面板
        Transform optionsPanel = currentPanel.GetCurrentOptionsPanel();
        if (optionsPanel == null)
        {
            Debug.LogError("无法获取选项面板！");
            return;
        }

        // 清空现有选项
        currentPanel.ClearOptions();
        // 加载并创建选项按钮
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            option.index = i;
            ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/UI/UIItem/DialogueOptionItem", optionsPanel,
                optionObj =>
                {
                    if (optionObj == null)
                    {
                        Debug.LogError($"选项预制体加载失败！索引: {i}");
                        return;
                    }

                    var optionItem = optionObj.GetComponent<DialogueOptionItem>();
                    if (optionItem != null)
                    {
                        optionItem.SetupOption(option);
                    }
                    else
                    {
                        Debug.LogError($"DialogueOptionItem 组件未找到！索引: {i}");
                    }
                });
        }
    }

    /// <summary>
    /// 选项被选择
    /// </summary>
    public void OnOptionSelected(DialogueOption option)
    {
        if (option == null)
        {
            Debug.LogError("选项为空！");
            return;
        }

        // 清空当前队列
        dialogueQueue.Clear();
        // 检查特殊值：-1 表示结束对话
        if (option.nextLineIndex == -1)
        {
            if (option.isTriggerEvent)
            {
                // 触发事件
                EventMgr.Instance.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(currentDialogue.id));
            }

            EndDialogue();
            return;
        }

        // 跳转到指定行
        if (currentDialogue != null && option.nextLineIndex >= 0 && option.nextLineIndex < currentDialogue.lines.Count)
        {
            // 从指定行开始重新填充队列
            for (int i = option.nextLineIndex; i < currentDialogue.lines.Count; i++)
            {
                dialogueQueue.Enqueue(currentDialogue.lines[i]);
            }

            currentLineIndex = option.nextLineIndex;
            isTyping = false;
            // 立即显示下一句
            CancelInvoke(nameof(DisplayNextLine));
            DisplayNextLine();
        }
        else
        {
            Debug.LogWarning($"无效的对话行索引: {option.nextLineIndex}");
            EndDialogue();
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    private void EndDialogue()
    {
        // 停止所有协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (waitForClickCoroutine != null)
        {
            StopCoroutine(waitForClickCoroutine);
            waitForClickCoroutine = null;
        }

        // 取消所有 Invoke
        CancelInvoke();
        // 关闭面板
        if (currentPanel != null)
        {
            currentPanel.ClosePanel();
            currentPanel = null;
        }

        // 重置状态
        currentDialogue = null;
        currentLineIndex = 0;
        dialogueQueue.Clear();
        isTyping = false;
        isWaitingClickForEnd = false;
        EventMgr.Instance.EventTrigger(GameEvent.CursorHide);
    }

    /// <summary>
    /// 判断是否正在对话
    /// </summary>
    private bool IsDialogueActive()
    {
        return currentPanel != null && currentPanel.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// 跳过当前对话
    /// </summary>
    private void SkipCurrentDialogue()
    {
        if (currentDialogue != null && currentDialogue.canSkip)
        {
            EndDialogue();
        }
    }

    private void Update()
    {
        // 提前返回，避免不必要的检查
        if (!IsDialogueActive()) return;
        // 空格键继续（只在非等待结束且无选项时）
        if (Input.GetKeyDown(KeyCode.Space) && !isWaitingClickForEnd && !currentPanel.HasOptions() &&
            Time.time - lastSpacePressTime >= SPACE_COOLDOWN)
        {
            lastSpacePressTime = Time.time;
            DisplayNextLine();
        }

        // ESC键跳过（只在非等待结束且无选项时）
        if (Input.GetKeyDown(KeyCode.Escape) && !isWaitingClickForEnd && !currentPanel.HasOptions())
        {
            SkipCurrentDialogue();
        }
    }

    /// <summary>
    /// 清理资源（当对象被销毁时）
    /// </summary>
    protected void OnDestroy()
    {
        EndDialogue();
    }
}