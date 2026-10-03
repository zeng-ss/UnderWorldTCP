using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;


public class DataUtils : Singleton<DataUtils>
{
    //登录时,缓存登录的时间毫秒..
    public Dictionary<string, long> _loginTimeDic = new Dictionary<string, long>();

    /// <summary>
    /// 添加登录毫秒值
    /// </summary>
    public void AddLoginMilliseconds(string username, long milliseconds)
    {
        if (!_loginTimeDic.ContainsKey(username))
        {
            _loginTimeDic.Add(username, milliseconds);
        }
    }

    public long GetLoginMilliseconds(string username)
    {
        if (_loginTimeDic.ContainsKey(username))
        {
            return _loginTimeDic[username];
        }

        return 0;
    }

    /*/// <summary>
    /// 验证中国大陆手机号（11位，以1开头）
    /// 支持最新号段：13/14/15/16/17/18/19
    /// </summary>
    public static bool IsValidMobile(string phone)
    {
        // 正则表达式解析：
        // ^1          以1开头
        // [3-9]       第二位为3-9
        // \d{9}$      后续9位数字
        const string pattern = @"^1[3-9]\d{9}$";
        return Regex.IsMatch(phone, pattern);
    }*/

    /// <summary>
    /// 验证账号是否合法
    /// </summary>
    public static bool IsValidUserName(string username)
    {
        // 正则表达式：以字母开头，后面可以跟任意数量的字母、数字或下划线
        string pattern = @"^[A-Za-z][A-Za-z0-9_]*$";
        return Regex.IsMatch(username, pattern);
    }

    /// <summary>
    /// 验证账号是否合法
    /// </summary>
    public static string GetMD5Hash(string input)
    {
        // 使用MD5创建哈希对象
        using (MD5 md5Hash = MD5.Create())
        {
            // 将输入字符串转换为字节数组并计算哈希
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // 创建一个新的Stringbuilder来收集字节并创建字符串
            StringBuilder sBuilder = new StringBuilder();

            // 循环字节数组并格式化每个字节为两个十六进制数字
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }

    /// <summary>
    /// 验证场景是否合法
    /// </summary>
    private static readonly string[] ValidSceneName =
    {
        "StartScene", "LobbyScene", "GameScene"
    };

    public static bool IsValidScene(string sceneName) => ValidSceneName.Contains(sceneName);
}