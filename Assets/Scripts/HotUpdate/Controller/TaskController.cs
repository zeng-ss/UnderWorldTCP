using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 任务 Controller：负责面板开关与「领奖」流程编排。
    /// </summary>
    public class TaskController
    {
        private bool _initialized;
        private TaskPanel _taskPanel;

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
            _taskPanel ??= AppContext.Ui.GetPanel<TaskPanel>();
            if (_taskPanel == null)
            {
                AppContext.Ui.OpenPanel<TaskPanel>();
                return;
            }

            if (_taskPanel.gameObject.activeInHierarchy) _taskPanel.ClosePanel();
            else AppContext.Ui.OpenPanel<TaskPanel>();
        }

        /// <summary>
        /// 处理面板上报的「领取奖励」：请服务端校验并发放（奖励已写入 role_bag_item），
        /// 之后再回拉一次背包，把新到手的驱动盘（含等级 / 词条）与材料数量落到本地。
        /// </summary>
        public void RequestFinish(TaskDataRuntime task)
        {
            if (!AppContext.Task.CanClaim(task)) return;

            AppContext.Proto.RequestGetReward(task.TaskId, ret =>
            {
                if (ret == null || ret.CmdCode != CmdCode.Succeed) return;

                RefreshBagFromServer();
                AppContext.Task.Claim(task);
                if (!string.IsNullOrEmpty(ret.RewardDesc)) AppContext.Ui.ShowTip(ret.RewardDesc, 4f);
            });
        }

        /// <summary>回拉服务端背包并覆盖本地（奖励发放 / 升级后都可能需要）</summary>
        private static void RefreshBagFromServer()
        {
            AppContext.Proto.RequestBagInfo(ret =>
            {
                if (ret == null || ret.CmdCode != CmdCode.Succeed) return;
                AppContext.Depot.ApplyServerBag(ret.DriverDiskMap.Values);
                AppContext.Material.ApplyServerBag(ret.MaterialMap.Values);
            });
        }
    }
}