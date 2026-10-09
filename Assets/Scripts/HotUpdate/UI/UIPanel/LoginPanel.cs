using System.Collections.Generic;
using HotUpdate.Controller;
using HotUpdate.Manager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 登录面板（纯 View）。
    ///
    /// 只负责：渲染服务器下拉框与状态、收集账号密码、把意图交给 LoginController。
    /// 网络请求、校验、建角色、跳面板全部在 Controller 里。
    ///
    /// 依赖方向是单向的：本面板持有 Controller 实例（自己 new），
    /// Controller 通过 AppContext.Ui.GetPanel&lt;LoginPanel&gt;() 直接把服务器列表写回来。
    /// </summary>
    [PanelPath("LoginPanel")]
    public class LoginPanel : BasePanel
    {
        [FormerlySerializedAs("StateText")] public Text stateText;
        public Dropdown dropdown;
        public Button loginBtn;
        public Button registerBtn;
        public Text account;
        public Text password;

        /// <summary>本面板的控制器，由面板自己 new 并持有</summary>
        private LoginController _controller;

        /// <summary>回写下拉框时抑制回调，避免和用户的真实选择互相触发</summary>
        private bool _syncingDropdown;

        protected override void Awake()
        {
            base.Awake();
            _controller = new LoginController();
            loginBtn.onClick.AddListener(RaiseLoginClicked);
            registerBtn.onClick.AddListener(RaiseRegisterClicked);
            dropdown.onValueChanged.AddListener(RaiseServerSelected);
        }

        private void OnEnable()
        {
            _controller.OnPanelShown();
        }

        #region 渲染（Model → View）

        /// <summary>由 LoginController 直接调用，渲染服务器下拉框与运行状态</summary>
        public void RefreshServerList(IReadOnlyList<string> serverNames, int selectedIndex, string runStateText,
            Color stateColor)
        {
            var options = new List<Dropdown.OptionData>(serverNames.Count);
            foreach (var name in serverNames) options.Add(new Dropdown.OptionData(name));
            dropdown.options = options;

            _syncingDropdown = true;
            dropdown.value = selectedIndex;
            _syncingDropdown = false;

            stateText.text = runStateText;
            stateText.color = stateColor;
        }

        #endregion

        #region 上报意图（View → Controller）

        private void RaiseServerSelected(int index)
        {
            if (_syncingDropdown) return;
            _controller.SelectServer(index);
        }

        private void RaiseLoginClicked()
        {
            _controller.Login(account.text.Trim(), password.text.Trim());
        }

        private void RaiseRegisterClicked()
        {
            _controller.RequestRegister();
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            loginBtn.onClick.RemoveAllListeners();
            registerBtn.onClick.RemoveAllListeners();
            dropdown.onValueChanged.RemoveAllListeners();
        }
    }
}
