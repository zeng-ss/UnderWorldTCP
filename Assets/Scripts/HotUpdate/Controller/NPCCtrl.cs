using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NpcCtrl : MonoBehaviour
{
    public List<DialogueData> dialogueDatas;
    private new Camera _camera;
    private TMP_Text _tipText;
    [HideInInspector] public bool isEnter;

    public void Start()
    {
        _camera = Camera.main;
        _tipText = GetComponentInChildren<TMP_Text>();
        _tipText.gameObject.SetActive(false);
        AppContext.Events.AddEventListener(GameEvent.DialogueEnd, GetTask);
        InputManager.Instance.RegisterGameplayKeyDown(KeyCode.F, OnInteractPressed);
    }

    private void Update()
    {
        // 提示文字始终朝向相机（每帧的视觉更新，不涉及输入）
        if (_tipText.gameObject.activeSelf)
        {
            _tipText.transform.LookAt(_camera.transform.position);
            _tipText.transform.Rotate(0, 180, 0);
        }
    }

    /// <summary>按 F 与 NPC 对话。注册到 InputManager，不再在 Update 里轮询。</summary>
    private void OnInteractPressed()
    {
        if (!isEnter) return;

        int index = AppContext.Story.DialogueIndex;
        if (index < 0 || index >= dialogueDatas.Count)
        {
            Debug.LogWarning($"对话下标越界：{index}，共 {dialogueDatas.Count} 段");
            return;
        }

        AppContext.Events.EventTrigger(GameEvent.CursorShow);
        AppContext.Dialogue.StartDialogue(dialogueDatas[index]);
    }

    /// <summary>
    /// 对话结束获取任务
    /// </summary>
    /// <param name="args">事件参数，实际类型为 DialogueEndArgs</param>
    private void GetTask(EventArgs args)
    {
        int dialogueId = ((DialogueEndArgs)args).DialogueId;
        // 走 TaskService.Unlock 做状态迁移（Locked → InProgress），不再直接改字段
        foreach (var task in AppContext.Task.Tasks.Where(task =>
                     dialogueDatas[AppContext.Story.DialogueIndex].taskIds.Contains(task.TaskId)))
        {
            AppContext.Task.Unlock(task.TaskId);
        }

        if (dialogueId != 2)
        {
            // 对话结束生成敌人
            //GameManager.Instance.SpawnEnemy();
            Vector3 pos = new Vector3(17, -1.6f, -30);
            AppContext.Proto.RequestSpawnEnemy(AppContext.Session.RoleId, 1, 20000, pos,
                AppContext.RemotePlayer.SpawnEnemy);
        }

        AppContext.Ui.OpenPanel<TipPanel>(panel => { panel.ShowTip("有新任务了，快去完成吧~"); });
        AppContext.Ui.OpenPanel<TaskPanel>((panel => { panel.RefreshTaskUI(AppContext.Task.Tasks); }));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isEnter = true;
            _tipText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isEnter = false;
            _tipText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        AppContext.Events.RemoveEventListener(GameEvent.DialogueEnd, GetTask);
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.F, OnInteractPressed);
    }
}