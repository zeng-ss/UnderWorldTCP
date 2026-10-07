using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr : Singleton<LubanMgr>
{
    // 各张配置表的字典缓存
    private Dictionary<int, SkillInfo> _skillInfoDic;
    private Dictionary<int, taskData> _taskDataDic;

    public void Init()
    {
        // 读取 luban 导出的 byte 表文件
        Tables tables = new Tables(file => new Luban.ByteBuf(File.ReadAllBytes(ResolveTablePath(file))));

        _skillInfoDic = tables.TbSkillInfo.DataMap;
        _taskDataDic = tables.TbtaskData.DataMap;
    }

    // 读取单张 byte 表的完整路径：优先在可执行文件所在目录下查找，找不到再向上级目录回退
    // （覆盖 Network 作为类库被 CenterServer/LoginServer 引用时，bytes 可能落在不同输出目录的情况）
    private static string ResolveTablePath(string file)
    {
        string fileName = $"{file}.bytes";
        string[] candidates =
        {
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "LubanManager", "Tb", fileName),
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, fileName),
            Path.Combine(System.IO.Directory.GetCurrentDirectory(), "LubanManager", "Tb", fileName),
            Path.Combine(System.IO.Directory.GetCurrentDirectory(), fileName),
        };

        foreach (string p in candidates)
        {
            if (File.Exists(p))
            {
                return p;
            }
        }

        throw new FileNotFoundException($"找不到 luban 表文件 {fileName}，已尝试: {string.Join("; ", candidates)}");
    }

    #region 技能相关的方法

    // 获取技能数据
    public Dictionary<int, SkillInfo> GetSkillInfos()
    {
        return _skillInfoDic;
    }

    // 通过技能 id 获取技能信息
    public SkillInfo GetSkillInfoById(int skillId)
    {
        if (_skillInfoDic.TryGetValue(skillId, out var skillInfoById))
        {
            return skillInfoById;
        }

        return null;
    }

    // 通过职业 id 获取所有该职业的技能信息
    public Dictionary<int, SkillInfo> GetSkillInfoByJobId(int jobId)
    {
        Dictionary<int, SkillInfo> jobSkillInfos = new Dictionary<int, SkillInfo>();
        foreach (var item in _skillInfoDic)
        {
            if (item.Value.JobId == jobId)
            {
                jobSkillInfos.Add(item.Key, item.Value);
            }
        }

        return jobSkillInfos;
    }

    #endregion

    #region 任务相关的方法

    // 获取所有任务配置
    public Dictionary<int, taskData> GetTaskDatas()
    {
        return _taskDataDic;
    }

    // 通过任务 id 获取任务配置
    public taskData GetTaskDataById(int taskId)
    {
        if (_taskDataDic != null && _taskDataDic.TryGetValue(taskId, out var taskDataById))
        {
            return taskDataById;
        }

        return null;
    }

    // 获取所有默认解锁的任务 id 列表
    public List<int> GetDefaultUnlockTaskIds()
    {
        List<int> ids = new List<int>();
        foreach (var item in _taskDataDic)
        {
            if (item.Value.IsUnlock)
            {
                ids.Add(item.Key);
            }
        }

        return ids;
    }

    #endregion

    #region 对话相关的方法（预留，后续对话表接入后填充）

    // TODO: 对话系统表接入后，在此添加对话数据加载与查询接口

    #endregion
}