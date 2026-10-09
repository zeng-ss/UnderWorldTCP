using System.Collections.Generic;
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

        /// <summary>处理面板上报的「领取奖励」：先请服务端校验并发放，成功后再落地本地状态</summary>
        public void RequestFinish(TaskDataRuntime task)
        {
            if (!AppContext.Task.CanClaim(task)) return;

            AppContext.Proto.RequestGetReward(task.TaskId, ret =>
            {
                if (ret == null || ret.CmdCode != CmdCode.Succeed) return;

                GrantRewardsLocally(ret.RewardMap);
                AppContext.Task.Claim(task);
                if (!string.IsNullOrEmpty(ret.RewardDesc)) AppContext.Ui.ShowTip(ret.RewardDesc, 4f);
            });
        }

        /// <summary>
        /// 把服务端下发的奖励明细落到本地背包（驱动盘按模板发放，材料累加数量）。
        /// TODO: 背包改为完全由服务端下发后，本方法与 DepotService/MaterialService 的本地发放入口一起删除。
        /// </summary>
        private static void GrantRewardsLocally(IEnumerable<KeyValuePair<int, int>> rewardMap)
        {
            foreach (var kv in rewardMap)
            {
                int itemId = kv.Key;
                int count = kv.Value;
                if (ItemCatalog.IsDriverDisk(itemId)) AppContext.Depot.AddByDepotId(itemId, count);
                else AppContext.Material.Add(itemId, count);
            }
        }
    }
}