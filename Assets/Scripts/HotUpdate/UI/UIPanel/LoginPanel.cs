using System.Collections.Generic;
using HotUpdate.Controller;
using HotUpdate.Core;
using HotUpdate.Event;
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
    /// Controller 不认识本面板，服务器列表通过 ServerListChanged 事件送回来。
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
            AppContext.Events.AddEventListener(GameEvent.ServerListChanged, OnServerListChanged);
            _controller.OnPanelShown();
        }

        private void OnDisable()
        {
            AppContext.Events.RemoveEventListener(GameEvent.ServerListChanged, OnServerListChanged);
        }

        #region 渲染（Model → View）

        private void OnServerListChanged(EventArgs args)
        {
            if (args is not ServerListArgs a) return;

            var options = new List<Dropdown.OptionData>(a.ServerNames.Count);
            foreach (var name in a.ServerNames) options.Add(new Dropdown.OptionData(name));
            dropdown.options = options;

            _syncingDropdown = true;
            dropdown.value = a.SelectedIndex;
            _syncingDropdown = false;

            stateText.text = a.StateText;
            stateText.color = a.StateColor;
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
