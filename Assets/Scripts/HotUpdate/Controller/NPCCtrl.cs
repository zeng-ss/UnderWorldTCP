using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.Manager;
using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEngine;

namespace HotUpdate.Controller
{
    public class NpcCtrl : MonoBehaviour
    {
        private Camera _camera;
        private TMP_Text _tipText;
        private bool _isEnter;

        public void Start()
        {
            _camera = Camera.main;
            _tipText = GetComponentInChildren<TMP_Text>();
            _tipText.gameObject.SetActive(false);
            AppContext.Events.AddEventListener(GameEvent.DialogueEnd, OnDialogueEnd);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.F, OnInteractPressed);
        }

        private void Update()
        {
            // 提示文字始终朝向相机
            if (_tipText.gameObject.activeSelf)
            {
                _tipText.transform.LookAt(_camera.transform.position);
                _tipText.transform.Rotate(0, 180, 0);
            }
        }

        private void OnInteractPressed()
        {
            if (!_isEnter) return;
            // 当前该播哪段对话由 StoryService 决定
            FindAnyObjectByType<DialogueManager>().StartDialogue();
        }

        /// <summary>
        /// 一段对话结束后：解锁该段对话配置的任务，并在非结局对话后生成敌人。
        /// </summary>
        private void OnDialogueEnd(EventArgs args)
        {
            if (args is not DialogueEndArgs endArgs || endArgs.Dialogue == null) return;
            DialogueData dialogue = endArgs.Dialogue;

            // 解锁本段对话配置的任务（Locked → InProgress）
            AppContext.Task.UnlockAll(dialogue.taskIds);

            // id == 2 为纯剧情收尾，不生成敌人
            if (dialogue.id != 2)
            {
                Vector3 pos = new Vector3(17, -1.6f, -30);
                AppContext.Proto.RequestSpawnEnemy(AppContext.Session.RoleId, 1, 20000, pos,
                    AppContext.RemotePlayer.SpawnEnemy);
            }

            AppContext.Ui.ShowTip("有新任务了，快去完成吧~");
            AppContext.Ui.OpenPanel<TaskPanel>(panel => panel.RefreshTaskUI(AppContext.Task.Tasks));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isEnter = true;
                _tipText.gameObject.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isEnter = false;
                _tipText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            AppContext.Events.RemoveEventListener(GameEvent.DialogueEnd, OnDialogueEnd);
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.F, OnInteractPressed);
        }
    }
}