using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RemotePlayerManager
{
    private readonly Dictionary<int, RemotePlayer> _remotePlayers = new();
    private readonly Dictionary<int, RemoteEnemy> _remoteEnemys = new();

    public void Init()
    {
        AppContext.Proto.OnPlayerEnterScene += OnPlayerEnterScene;
        AppContext.Proto.OnPositionSyncReceived += OnPositionSync;
        AppContext.Proto.OnPlayerLeaveScene += OnPlayerLeaveScene;
        AppContext.Proto.OnPlayerAttackBroadcast += OnPlayerAttackBroadcast;
        AppContext.Proto.OnSyncAniReceived += OnSyncAni;
        AppContext.Proto.OnPlayerVfxReceived += OnPlayerVfxReceived;
        AppContext.Proto.OnEnemyPositionSyncReceived += OnEnemyPositionSync;
        AppContext.Proto.OnEnemySyncAniReceived += OnSyncEnemyAni;
    }

    private void OnPlayerVfxReceived(PlayerVfxNtf ntf)
    {
        if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
        {
            rp.Ctrl.SkillCombo.SpawnRemoteVfx(ntf.SkillConfigIndex, ntf.AttackIndex, ntf.VfxIndex);
        }
    }

    private void OnPlayerEnterScene(PlayerEnterSceneNtf ntf)
    {
        if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer existed))
        {
            if (existed != null) return;
            _remotePlayers.Remove(ntf.RoleId);
        }

        Vector3 pos = new Vector3(ntf.PosX, ntf.PosY, ntf.PosZ);
        AppContext.Res.LoadAndInstantiateAsync("Character", null, obj =>
        {
            // 标记为非本地玩家，禁用 CharacterController 防止与位置插值冲突
            var pc = obj.GetComponent<PlayerCtrl>();
            if (pc != null)
            {
                pc.Core.IsLocalPlayer = false;
                pc.CharacterController.enabled = false;
            }

            var rp = obj.AddComponent<RemotePlayer>();
            rp.Init(ntf.RoleId, ntf.Nickname, pos);
            _remotePlayers[ntf.RoleId] = rp;

            Debug.Log($"[RemotePlayerManager] 玩家进入: roleId={ntf.RoleId} nickname={ntf.Nickname}");
        });
    }

    private void OnPositionSync(PositionSyncNtf ntf)
    {
        if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
        {
            rp.targetPos = new Vector3(ntf.PosX, ntf.PosY, ntf.PosZ);
            rp.targetRotation = Quaternion.Euler(0, ntf.RotationY, 0);
        }
    }

    // 服务端广播「别人」的动画同步（请求者自己收不到回包，因此只由事件触发）
    private void OnSyncAni(SyncAniRet ret)
    {
        if (_remotePlayers.TryGetValue(ret.RoleId, out RemotePlayer rp))
        {
            // 连招序号在协议里是裸 int：先校验成合法枚举再进玩法层，非法值直接丢弃
            if (System.Enum.IsDefined(typeof(ComboSet), ret.SkillConfigIndex))
                rp.Ctrl.SkillCombo.UpdateSkillConfig((ComboSet)ret.SkillConfigIndex);
            rp.Ctrl.playerModel.Animator.CrossFadeInFixedTime(ret.AnimationName, 0.1f, 0, 0f);
        }
    }

    private void OnPlayerLeaveScene(PlayerLeaveSceneNtf ntf)
    {
        if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
        {
            GameObject.Destroy(rp.gameObject);
            _remotePlayers.Remove(ntf.RoleId);
            Debug.Log($"[RemotePlayerManager] 玩家离开: roleId={ntf.RoleId}");
        }
    }

    // 别人的攻击广播 → 同步敌人状态 + 攻击者动画
    private void OnPlayerAttackBroadcast(PlayerAttackRet ret)
    {
        if (EnemyCtrl.Instances.TryGetValue(ret.EnemyInstanceId, out EnemyCtrl enemy))
        {
            enemy.OnServerAttackResult(ret);
        }
    }

    public void ResetForNewScene()
    {
        foreach (var kv in _remotePlayers)
        {
            if (kv.Value != null) GameObject.Destroy(kv.Value.gameObject);
        }

        _remotePlayers.Clear();
    }


    public void SpawnEnemy(SpawnEnemyRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            Debug.LogError("生成敌人失败：" + ret.CmdCode);
            return;
        }

        Vector3 spawnPos = new Vector3(ret.PosX, ret.PosY, ret.PosZ);
        AppContext.Res.LoadAndInstantiateAsync("enemy", null, enemy =>
        {
            enemy.transform.position = spawnPos;
            EnemyCtrl ctrl = enemy.GetComponent<EnemyCtrl>();
            if (ctrl)
            {
                ctrl.IsServer = false;
                ctrl.serverInstanceId = ret.EnemyInstanceId;
                ctrl.roleId = ret.RoleId;
                ctrl.maxHealthValue = ret.MaxHp;
                ctrl.networkHealth = ret.CurrHp;
                ctrl.PlayerRef = Object.FindAnyObjectByType<PlayerCtrl>();
                ctrl.fillImage.fillAmount = ret.CurrHp / ret.MaxHp;
                ctrl.healthText.text = $"{ret.CurrHp}/{ret.MaxHp}";
                EnemyCtrl.Instances[ret.EnemyInstanceId] = ctrl;
            }

            var re = enemy.AddComponent<RemoteEnemy>();
            re.Init(ret.RoleId, ret.EnemyInstanceId, spawnPos);
            _remoteEnemys.Add(ret.RoleId, re);
        });
    }

    private void OnEnemyPositionSync(EnemyPositionSyncRet ret)
    {
        if (_remoteEnemys.TryGetValue(ret.RoleId, out RemoteEnemy re))
        {
            re.targetPos = new Vector3(ret.PosX, ret.PosY, ret.PosZ);
            re.targetRotation = Quaternion.Euler(0, ret.RotationY, 0);
        }
    }

    private void OnSyncEnemyAni(EnemySyncAniRet ret)
    {
        if (_remotePlayers.TryGetValue(ret.RoleId, out RemotePlayer rp))
        {
            rp.Ctrl.playerModel.Animator.CrossFadeInFixedTime(ret.AnimationName, 0.1f, 0, 0f);
        }
    }


    public void Clear()
    {
        if (!AppContext.IsAlive) return;

        AppContext.Proto.OnPlayerEnterScene -= OnPlayerEnterScene;
        AppContext.Proto.OnPositionSyncReceived -= OnPositionSync;
        AppContext.Proto.OnPlayerLeaveScene -= OnPlayerLeaveScene;
        AppContext.Proto.OnPlayerAttackBroadcast -= OnPlayerAttackBroadcast;
        AppContext.Proto.OnSyncAniReceived -= OnSyncAni;
        AppContext.Proto.OnPlayerVfxReceived -= OnPlayerVfxReceived;
        AppContext.Proto.OnEnemyPositionSyncReceived -= OnEnemyPositionSync;
        AppContext.Proto.OnEnemySyncAniReceived -= OnSyncEnemyAni;
    }
}