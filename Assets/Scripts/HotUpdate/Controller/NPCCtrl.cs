using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NPCCtrl : MonoBehaviour
{
    public List<DialogueData> dialogueDatas;
    private new Camera camera;
    private TMP_Text tipText;
    [HideInInspector] public bool isEnter;

    public void Start()
    {
        camera = Camera.main;
        tipText = GetComponentInChildren<TMP_Text>();
        tipText.gameObject.SetActive(false);
        EventMgr.Instance.AddEventListener(GameEvent.DialogueEnd, GetTask);
    }

    private void Update()
    {
        if (tipText.gameObject.activeSelf)
        {
            tipText.transform.LookAt(camera.transform.position);
            tipText.transform.Rotate(0, 180, 0);
        }

        if (Input.GetKeyDown(KeyCode.F) && isEnter)
        {
            if (GameManager.Instance.dialogueId >= dialogueDatas.Count || GameManager.Instance.dialogueId < 0)
            {
                print("大概率是超出下标范围");
                return;
            }

            EventMgr.Instance.EventTrigger(GameEvent.CursorShow);
            DialogueManager.Instance.StartDialogue(dialogueDatas[GameManager.Instance.dialogueId]);
        }
    }

    /// <summary>
    /// 对话结束获取任务
    /// </summary>
    /// <param name="args">事件参数，实际类型为 DialogueEndArgs</param>
    private void GetTask(EventArgs args)
    {
        int dialogueId = ((DialogueEndArgs)args).DialogueId;
        foreach (var task in GameManager.Instance.curTasksData.Where(task =>
                     dialogueDatas[GameManager.Instance.dialogueId].taskIds.Contains(task.taskId)))
        {
            task.isUnlock = true; // 解锁对应任务
        }

        if (dialogueId != 2)
        {
            // 对话结束生成敌人
            //GameManager.Instance.SpawnEnemy();
            Vector3 pos = new Vector3(17, -1.6f, -30);
            ProtoHandler.Instance.RequestSpawnEnemy(GameManager.Instance.roleId, 1, 20000, pos,
                RemotePlayerManager.Instance.SpawnEnemy);
        }

        UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("有新任务了，快去完成吧~"); });
        UIManager.Instance.OpenPanel<TaskPanel>((panel => { panel.RefreshTaskUI(GameManager.Instance.curTasksData); }));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isEnter = true;
            tipText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isEnter = false;
            tipText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(GameEvent.DialogueEnd, GetTask);
    }
}