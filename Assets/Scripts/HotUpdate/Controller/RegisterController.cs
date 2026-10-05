/// <summary>
/// 注册流程 Controller。普通类，由 RegisterPanel 自己 new 并持有，不是单例。
/// 校验规则与网络请求原先写在 RegisterPanel 里。
///
/// 不持有面板引用：面板直接调用 Register / BackToLogin，
/// 结果写进 Service，要提示就直接调 UIManager.ShowTip。
/// </summary>
public class RegisterController
{
    #region View → Controller

    public void Register(string account, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
        {
            ShowTip("账号和密码不能为空");
            return;
        }

        if (password != confirmPassword)
        {
            ShowTip("密码输入不一致，请重新输入");
            return;
        }

        if (password.Length is < 4 or > 16)
        {
            ShowTip("密码长度需要4~16位");
            return;
        }

        AppContext.Proto.RequestRegist(account, "", password, OnRegisterResult);
    }

    public void BackToLogin()
    {
        AppContext.Ui.OpenPanel<LoginPanel>();
        AppContext.Ui.ClosePanel<RegisterPanel>();
    }

    #endregion

    private void OnRegisterResult(RegistRet ret)
    {
        switch (ret.CmdCode)
        {
            case CmdCode.Succeed:
                ShowTip("注册成功");
                AppContext.Ui.ClosePanel<RegisterPanel>();
                AppContext.Ui.OpenPanel<LoginPanel>();
                break;
            case CmdCode.AcctExist:
                ShowTip("用户名已存在");
                break;
            default:
                ShowTip("注册失败，服务器错误");
                break;
        }
    }

    private void ShowTip(string message)
    {
        AppContext.Ui.ShowTip(message);
    }
}
