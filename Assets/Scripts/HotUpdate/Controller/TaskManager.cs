using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 任务管理器- 所有任务逻辑入口
/// </summary>
public class TaskManager : UnitySingleTonMono<TaskManager>
{
    private TaskPanel taskPanel; // 任务面板引用
    private void Start()
    {
        // 初始化面板
        UIManager.Instance.OpenPanel<TaskPanel>(panel =>
        {
            taskPanel = panel;
            taskPanel.transform.localScale = Vector3.zero;
            UIManager.Instance.ClosePanel<TaskPanel>();
        });
    }
    
    /// <summary>
    /// 更新任务进度 - 外部调用入口
    /// </summary>
    public void UpdateTaskProgress(TaskType taskType, int addCount = 1)
    {
        if (!GameManager.Instance.taskConfigSO || GameManager.Instance.taskConfigSO.taskDataList == null) return;
        bool isProgressChanged = false; // 标记进度是否真的变化
        foreach (var task in GameManager.Instance.curTasksData)
        {
            if (task.isFinished || !task.isUnlock || task.taskType != taskType) continue;
            int oldCount = task.currentCount;
            task.currentCount += addCount;
            task.currentCount = Mathf.Min(task.currentCount, task.targetCount);
            // 只有进度变化时才标记
            if (task.currentCount != oldCount)
            {
                isProgressChanged = true;
            }
        }
        // 只在进度变化时刷新UI
        if (isProgressChanged && taskPanel != null)
        {
            taskPanel.RefreshTaskUI(GameManager.Instance.curTasksData);
        }
    }

    #region 任务完成判定 奖励发放
    
    /// <summary>
    /// 检查任务是否完成
    /// </summary>
    public void CheckTaskFinish(TaskDataRuntime task)
    {
        if (task.currentCount >= task.targetCount && !task.isFinished)
        {
            GameManager.Instance.dialogueId++;
            UIManager.Instance.OpenPanel<TaskPanel>();
            task.isFinished = true;
            taskPanel.gameObject.SetActive(true);
            GiveTaskReward(task);
        }
    }

    /// <summary>
    /// 发放任务奖励（适配你的产值/金币系统，直接改这里即可）
    /// </summary>
    private void GiveTaskReward(TaskDataRuntime task)
    {
        if (task.taskReward == null) return;
        // 延迟到下一帧执行，脱离 DOTween回调上下文
        MonoBehaviour coroutineHost = FindObjectOfType<GameManager>(); // 用 GameManager作为协程宿主
        coroutineHost.StartCoroutine(DelayAddReward(task));
    }
    
    // 协程：延迟执行奖励添加
    private IEnumerator DelayAddReward(TaskDataRuntime task)
    {
        yield return null; // 延迟 1帧执行
        string des = "获得奖励\n";
        switch (task.taskType)
        {
            case TaskType.击败第一个敌人: 
                des += task.taskReward.AddDepotNum(true);
                des += task.taskReward.AddMaterialNum(); 
                ProtoHandler.Instance.RequestGetReward(1, ret =>
                {
                    UIManager.Instance.OpenPanel<TipPanel>(panel =>
                    {
                        panel.showTime = 4;
                        panel.ShowTip(des);
                    });
                });
                break;
            case TaskType.给每一个驱动盘都升一级: 
                task.taskReward.AddDepotNum();
                des += task.taskReward.AddMaterialNum(); 
                ProtoHandler.Instance.RequestGetReward(2, ret =>
                {
                    UIManager.Instance.OpenPanel<TipPanel>(panel =>
                    {
                        panel.showTime = 4;
                        panel.ShowTip(des);
                    });
                });
                break;
        }
    }
    
    #endregion

    // 快捷键打开/关闭任务面板（和你的TogglePanel逻辑一致）
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        if (taskPanel && taskPanel.gameObject.activeInHierarchy)
        {
            taskPanel.ClosePanel();
        }
        else
        {
            UIManager.Instance.OpenPanel<TaskPanel>((panel =>
            {
                taskPanel = panel;
                taskPanel.RefreshTaskUI(GameManager.Instance.curTasksData);
            }));
        }
    }
    
}