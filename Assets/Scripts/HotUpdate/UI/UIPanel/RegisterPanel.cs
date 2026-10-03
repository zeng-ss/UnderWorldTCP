using UnityEngine.UI;

public class RegisterPanel : BasePanel
{
    public Text account;
    public Text password;
    public Text pwsCheck;
    public Button registerBtn;
    public Button loginBtn;

    private void Start()
    {
        registerBtn.onClick.AddListener(RegisterCheck);
        loginBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.OpenPanel<LoginPanel>();
            UIManager.Instance.ClosePanel<RegisterPanel>();
        });
    }

    private void RegisterCheck()
    {
        string userName = account.text.Trim();
        string pwd = password.text.Trim();
        string pwdConfirm = pwsCheck.text.Trim();

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(pwd))
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("账号和密码不能为空"); });
            return;
        }

        if (pwd != pwdConfirm)
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("密码输入不一致，请重新输入"); });
            return;
        }

        if (pwd.Length is < 4 or > 16)
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("密码长度需要4~16位"); });
            return;
        }

        ProtoHandler.Instance.RequestRegist(userName, "", pwd, OnRegistResult);
    }

    private void OnRegistResult(RegistRet ret)
    {
        switch (ret.CmdCode)
        {
            case CmdCode.Succeed:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("注册成功"); });
                UIManager.Instance.ClosePanel<RegisterPanel>();
                UIManager.Instance.OpenPanel<LoginPanel>();
                break;
            case CmdCode.AcctExist:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("用户名已存在"); });
                break;
            default:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("注册失败，服务器错误"); });
                break;
        }
    }
}
