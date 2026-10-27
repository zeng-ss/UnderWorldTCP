using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr : Singleton<LubanMgr>
{
    // 各张配置表的字典缓存
    private Dictionary<int, taskData> _taskDataDic;
    private Dictionary<int, materialData> _materialDic;
    private Dictionary<int, driverDisk> _driverDiskDic;
    private Dictionary<int, enemyData> _enemyDic;
    private Dictionary<int, waveData> _waveDic;

    public void Init()
    {
        // 读取 luban 导出的 byte 表文件
        Tables tables = new Tables(file => new Luban.ByteBuf(File.ReadAllBytes(ResolveTablePath(file))));

        _taskDataDic = tables.TbtaskData.DataMap;
        _materialDic = tables.TbmaterialData.DataMap;
        _driverDiskDic = tables.TbdriverDisk.DataMap;
        _enemyDic = tables.TbenemyData.DataMap;
        _waveDic = tables.TbwaveData.DataMap;
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

    #region 敌人相关的方法

    // 通过编号获取敌人配置（血量 / 攻击 / 移动 / 掉落都在这张表里）
    public enemyData GetEnemyDataById(int enemyId)
    {
        if (_enemyDic != null && _enemyDic.TryGetValue(enemyId, out var enemyById))
        {
            return enemyById;
        }

        return null;
    }

    // 通过关卡 id 获取波次配置
    public waveData GetWaveDataById(int levelId)
    {
        if (_waveDic != null && _waveDic.TryGetValue(levelId, out var waveById))
        {
            return waveById;
        }

        return null;
    }

    #endregion

    #region 物品相关的方法（材料 / 驱动盘）

    // 获取所有材料配置
    public Dictionary<int, materialData> GetMaterialDatas()
    {
        return _materialDic;
    }

    // 通过编号获取材料配置
    public materialData GetMaterialById(int materialId)
    {
        if (_materialDic != null && _materialDic.TryGetValue(materialId, out var materialById))
        {
            return materialById;
        }

        return null;
    }

    // 获取所有驱动盘配置
    public Dictionary<int, driverDisk> GetDriverDisks()
    {
        return _driverDiskDic;
    }

    // 通过编号获取驱动盘配置
    public driverDisk GetDriverDiskById(int depotId)
    {
        if (_driverDiskDic != null && _driverDiskDic.TryGetValue(depotId, out var diskById))
        {
            return diskById;
        }

        return null;
    }

    #endregion

    #region 对话相关的方法（预留，后续服务端校验对话解锁时填充）

    // TODO: 服务端需要校验对话解锁时，在此添加对话数据查询接口

    #endregion
}