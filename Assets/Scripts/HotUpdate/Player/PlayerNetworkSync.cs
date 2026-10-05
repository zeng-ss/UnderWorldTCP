using UnityEngine;

public class PlayerNetworkSync
{
    public void SyncAnimation(string animationName, int skillIndex)
    {
        ProtoHandler.Instance.RequestSyncAni(AppContext.Session.RoleId, animationName, skillIndex,
            ret => { RemotePlayerManager.Instance.OnSyncAni(ret); });
    }

    public void SyncVfx(int roleId, int skillConfigIndex, int attackIndex, int vfxIndex)
    {
        ProtoHandler.Instance.RequestSyncVfx(new PlayerVfxNtf
        {
            RoleId = roleId,
            SkillConfigIndex = skillConfigIndex,
            AttackIndex = attackIndex,
            VfxIndex = vfxIndex
        });
    }

    /// <summary>攻击验伤请求（原 OnHit 的联网段，服务端权威）</summary>
    public void RequestAttack(EnemyCtrl enemy, float baseDamage, float baoJi, bool isExAttack)
    {
        ProtoHandler.Instance.RequestPlayerAttack(
            AppContext.Session.RoleId,
            enemy.serverInstanceId,
            baseDamage,
            baoJi,
            isExAttack,
            enemy.OnServerAttackResult
        );
    }

    /// <summary>position 走 root transform，rotation 走 model transform</summary>
    public void StartPositionSync(Transform rootTransform, Transform modelTransform)
    {
        ProtoHandler.Instance.StartPositionSync(rootTransform, AppContext.Session.RoleId, modelTransform);
    }

    public void StopPositionSync()
    {
        ProtoHandler.Instance.StopPositionSync();
    }
}