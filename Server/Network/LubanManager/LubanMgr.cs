using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr : Singleton<LubanMgr>
{
    private Dictionary<int, SkillInfo> _skillinfoDic;

    public void init()
    {
        //读取luban导出的byte表文件  
        Tables tables = new Tables((string file) =>
            new Luban.ByteBuf(
                File.ReadAllBytes(
                    $"F:\\技能实训班\\游戏技能实践班2026\\ARPGDemo_Server\\Network\\LubanManager\\Tb\\{file}.bytes")));
        _skillinfoDic = tables.TbSkillInfo.DataMap;
    }

    #region 技能相关的方法

    /// <summary>
    /// 获取技能数据 
    /// </summary>
    public Dictionary<int, SkillInfo> GetSkillInfos()
    {
        return _skillinfoDic;
    }

    /// <summary>
    /// 通过技能id获取技能信息 
    /// </summary>
    public SkillInfo GetSkillInfoById(int skillId)
    {
        if (_skillinfoDic.ContainsKey(skillId))
        {
            return _skillinfoDic[skillId];
        }

        return null;
    }

    /// <summary>
    /// 通过职业id获取所有该职业的技能信息 
    /// </summary>
    public Dictionary<int, SkillInfo> GetSkillInfoByJobId(int jobId)
    {
        Dictionary<int, SkillInfo> jobSkillInfos = new Dictionary<int, SkillInfo>();
        foreach (var item in _skillinfoDic)
        {
            if (item.Value.JobId == jobId)
            {
                jobSkillInfos.Add(item.Key, item.Value);
            }
        }

        return jobSkillInfos;
    }

    #endregion
}