using UnityEngine.UI;

/// <summary>
/// 注册面板（纯 View）。校验与网络请求移到了 RegisterController。
///
/// 依赖方向单向：本面板 new 并持有 Controller，Controller 不认识本面板。
/// </summary>
[PanelPath("Assets/Res/UI/UIPanel/RegisterPanel")]
public class RegisterPanel : BasePanel
{
    public Text account;
    public Text password;
    public Text pwsCheck;
    public Button registerBtn;
    public Button loginBtn;

    /// <summary>本面板的控制器，由面板自己 new 并持有</summary>
    private RegisterController _controller;

    protected override void Awake()
    {
        base.Awake();
        _controller = new RegisterController();
        registerBtn.onClick.AddListener(RaiseRegisterClicked);
        loginBtn.onClick.AddListener(RaiseBackToLoginClicked);
    }

    private void RaiseRegisterClicked()
    {
        _controller.Register(account.text.Trim(), password.text.Trim(), pwsCheck.text.Trim());
    }

    private void RaiseBackToLoginClicked()
    {
        _controller.BackToLogin();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        registerBtn.onClick.RemoveAllListeners();
        loginBtn.onClick.RemoveAllListeners();
    }
}
