using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>
/// 敌人实例管理器（内存中管理，不需要数据库表）
/// 联机时由服务端权威控制敌人的生成、受伤、死亡
/// </summary>
public class EnemyMgr : Singleton<EnemyMgr>
{
    private int _nextInstanceId = 1;
    private readonly Dictionary<int, EnemyInstance> enemy_Dict = new Dictionary<int, EnemyInstance>();

    /// <summary>
    /// 生成敌人
    /// </summary>
    public void SpawnEnemy(int roleId, int enemyConfigId, float maxHp, float posX, float posY, float posZ)
    {
        int id = _nextInstanceId++;
        SpawnEnemyRet ret = new SpawnEnemyRet
        {
            RoleId = roleId,
            EnemyInstanceId = id,
            EnemyConfigId = enemyConfigId,
            MaxHp = maxHp,
            CurrHp = maxHp,
            PosX = posX,
            PosY = posY,
            PosZ = posZ
        };
        enemy_Dict[id] = new EnemyInstance
        {
            InstanceId = id,
            RoleId = roleId,
            MaxHp = maxHp,
            CurrHp = maxHp,
        };

        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_SpawnEnemyCode, Data = ret.ToByteString() };
        foreach (var sessionId in PlayerSceneMgr.Instance.GetSessionIdsInScene())
        {
            SessionMgr.Instance.GetSession(sessionId).SendData(pkg);
            LogMsg.Info($"[EnemyMgr]敌人生成: instanceId={id} roleId={roleId} pos=({posX:F1},{posY:F1},{posZ:F1}) maxHp={maxHp}",ConsoleColor.Magenta);
        }
    }

    /// <summary>
    /// 处理玩家攻击，返回攻击结果
    /// </summary>
    public PlayerAttackRet ProcessAttack(PlayerAttackReq req)
    {
        PlayerAttackRet ret = new PlayerAttackRet
        {
            EnemyInstanceId = req.EnemyInstanceId,
            AttackerRoleId = req.RoleId
        };

        if (!enemy_Dict.TryGetValue(req.EnemyInstanceId, out EnemyInstance enemy))
        {
            ret.CmdCode = CmdCode.EnemyNotExist;
            return ret;
        }

        if (enemy.CurrHp <= 0)
        {
            ret.CmdCode = CmdCode.Succeed;
            ret.DamageDealt = 0;
            ret.EnemyCurrHp = 0;
            ret.EnemyMaxHp = enemy.MaxHp;
            ret.IsDead = true;
            return ret;
        }

        // 计算伤害
        float damage = req.Damage;
        bool isBaoji = false;
        if (new Random().Next(1, 101) <= req.BaojiPercent)
        {
            damage += new Random().Next(200, 1501);
            isBaoji = true;
        }

        enemy.CurrHp -= damage;
        bool isDead = enemy.CurrHp <= 0;
        if (isDead) enemy.CurrHp = 0;

        ret.CmdCode = CmdCode.Succeed;
        ret.DamageDealt = damage;
        ret.EnemyCurrHp = enemy.CurrHp;
        ret.EnemyMaxHp = enemy.MaxHp;
        ret.IsDead = isDead;
        ret.IsBaoji = isBaoji;

        LogMsg.Info($"[EnemyMgr]攻击处理: enemyId={req.EnemyInstanceId} damage={damage} hp={enemy.CurrHp}/{enemy.MaxHp} isDead={isDead} isBaoji={isBaoji}");
        return ret;
    }

    /// <summary>
    /// 移除敌人
    /// </summary>
    public void RemoveEnemy(int instanceId)
    {
        if (enemy_Dict.Remove(instanceId))
        {
            LogMsg.Info($"[EnemyMgr]敌人移除: instanceId={instanceId}");
        }
    }

    /// <summary>
    /// 清空某角色的所有敌人（切换场景时调用）
    /// </summary>
    public void ClearRoleEnemies(int roleId)
    {
        var toRemove = enemy_Dict.Where(kv => kv.Value.RoleId == roleId).Select(kv => kv.Key).ToList();
        foreach (int id in toRemove) enemy_Dict.Remove(id);
        LogMsg.Info($"[EnemyMgr]清除角色敌人: roleId={roleId} count={toRemove.Count}");
    }
}

public class EnemyInstance
{
    public int InstanceId;
    public int RoleId;
    public float MaxHp;
    public float CurrHp;
}