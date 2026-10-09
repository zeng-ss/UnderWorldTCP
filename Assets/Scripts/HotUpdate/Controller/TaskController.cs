using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 任务 Controller。
    /// </summary>
    public class TaskController
    {
        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            InputManager.Instance.RegisterKeyDown(KeyCode.Tab, TogglePanel);
        }

        public void Dispose()
        {
            if (!_initialized) return;
            _initialized = false;

            InputManager.Instance.UnregisterKeyDown(KeyCode.Tab, TogglePanel);
        }

        private void TogglePanel()
        {
            var panel = AppContext.Ui.GetPanel<TaskPanel>();
            if (panel != null && panel.gameObject.activeInHierarchy)
            {
                panel.ClosePanel();
                return;
            }

            AppContext.Ui.OpenPanel<TaskPanel>();
        }

        /// <summary>处理面板上报的「领取奖励」</summary>
        public void RequestFinish(TaskDataRuntime task)
        {
            if (!AppContext.Task.Claim(task, out string rewardText)) return;

            NotifyServer(task, rewardText);
        }

        private void NotifyServer(TaskDataRuntime task, string rewardText)
        {
            int rewardId = task.TaskType == TaskType.击败第一个敌人 ? 1 : 2;
            AppContext.Proto.RequestGetReward(rewardId, ret => { AppContext.Ui.ShowTip(rewardText, 4f); });
        }
    }
}
