using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理其他玩家的创建、位置更新、销毁
/// 通过 ProtoHandler 事件驱动，不直接依赖 SocketDispatcher
/// </summary>
public class RemotePlayerManager : UnitySingleTonMono<RemotePlayerManager>
{
    private Dictionary<int, RemotePlayer> _remotePlayers = new();
    private Dictionary<int, RemoteEnemy> _remoteEnemys = new();

    public override void Awake()
    {
        base.Awake();
        ProtoHandler.Instance.OnPlayerEnterScene += OnPlayerEnterScene;
        ProtoHandler.Instance.OnPositionSyncReceived += OnPositionSync;
        ProtoHandler.Instance.OnPlayerLeaveScene += OnPlayerLeaveScene;
        ProtoHandler.Instance.OnPlayerAttackBroadcast += OnPlayerAttackBroadcast;
        ProtoHandler.Instance.OnSyncAniReceived += OnSyncAni;
        ProtoHandler.Instance.OnPlayerVfxReceived += OnPlayerVfxReceived;
        ProtoHandler.Instance.OnEnemyPositionSyncReceived += OnEnemyPositionSync;
        ProtoHandler.Instance.OnEnemySyncAniReceived += OnSyncEnemyAni;
    }

    private void OnPlayerVfxReceived(PlayerVfxNtf ntf)
    {
        if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
        {
            rp.Ctrl.SpawnRemoteVfx(ntf.SkillConfigIndex, ntf.AttackIndex, ntf.VfxIndex);
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
        ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/Prefab/Character", null, obj =>
        {
            // 标记为非本地玩家，禁用 CharacterController 防止与位置插值冲突
            var pc = obj.GetComponent<PlayerCtrl>();
            if (pc != null)
            {
                pc.IsLocalPlayer = false;
                pc.characterController.enabled = false;
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
            rp.TargetPos = new Vector3(ntf.PosX, ntf.PosY, ntf.PosZ);
            rp.TargetRotation = Quaternion.Euler(0, ntf.RotationY, 0);
        }
    }

    public void OnSyncAni(SyncAniRet ret)
    {
        if (_remotePlayers.TryGetValue(ret.RoleId, out RemotePlayer rp))
        {
            rp.Ctrl.UpdateSkillConfig(ret.SkillConfigIndex);
            rp.Ctrl.playerModel.Animator.CrossFadeInFixedTime(ret.AnimationName, 0.1f, 0, 0f);
        }
    }

    private void OnPlayerLeaveScene(PlayerLeaveSceneNtf ntf)
    {
        if (_remotePlayers.TryGetValue(ntf.RoleId, out RemotePlayer rp))
        {
            Destroy(rp.gameObject);
            _remotePlayers.Remove(ntf.RoleId);
            Debug.Log($"[RemotePlayerManager] 玩家离开: roleId={ntf.RoleId}");
        }
    }

    /// <summary>
    /// 别人的攻击广播 → 同步敌人状态 + 攻击者动画
    /// </summary>
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
            if (kv.Value != null) Destroy(kv.Value.gameObject);
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
        ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/Prefab/enemy", null, enemy =>
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
                ctrl.PlayerRef = FindObjectOfType<PlayerCtrl>();
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
            re.TargetPos = new Vector3(ret.PosX, ret.PosY, ret.PosZ);
            re.TargetRotation = Quaternion.Euler(0, ret.RotationY, 0);
        }
    }

    private void OnSyncEnemyAni(EnemySyncAniRet ret)
    {
        if (_remotePlayers.TryGetValue(ret.RoleId, out RemotePlayer rp))
        {
            rp.Ctrl.playerModel.Animator.CrossFadeInFixedTime(ret.AnimationName, 0.1f, 0, 0f);
        }
    }


    private void OnDestroy()
    {
        if (ProtoHandler.Instance != null)
        {
            ProtoHandler.Instance.OnPlayerEnterScene -= OnPlayerEnterScene;
            ProtoHandler.Instance.OnPositionSyncReceived -= OnPositionSync;
            ProtoHandler.Instance.OnPlayerLeaveScene -= OnPlayerLeaveScene;
            ProtoHandler.Instance.OnPlayerAttackBroadcast -= OnPlayerAttackBroadcast;
            ProtoHandler.Instance.OnSyncAniReceived -= OnSyncAni;
            ProtoHandler.Instance.OnPlayerVfxReceived -= OnPlayerVfxReceived;
            ProtoHandler.Instance.OnEnemyPositionSyncReceived -= OnEnemyPositionSync;
            ProtoHandler.Instance.OnEnemySyncAniReceived -= OnSyncEnemyAni;
        }
    }
}

/// <summary>
/// 远程玩家组件：挂载在其他玩家的GameObject上，处理位置平滑插值
/// </summary>
public class RemotePlayer : MonoBehaviour
{
    public int RoleId { get; private set; }
    public string Nickname { get; private set; }
    public Vector3 TargetPos;
    public Quaternion TargetRotation;
    public float smoothSpeed = 10f;
    private Transform _modelTransform;
    public PlayerCtrl Ctrl { get; private set; }

    public void Init(int roleId, string nickname, Vector3 pos)
    {
        RoleId = roleId;
        Nickname = nickname;
        TargetPos = pos;
        TargetRotation = Quaternion.identity;
        Ctrl = GetComponent<PlayerCtrl>();

        // 找模型子节点（Character 预制体的 root/playerModel）
        _modelTransform = GetComponentInChildren<PlayerModel>().transform;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, TargetPos, smoothSpeed * Time.deltaTime);
        Transform rotTarget = _modelTransform ? _modelTransform : transform;
        rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, TargetRotation, smoothSpeed * Time.deltaTime);
    }
}

/// <summary>
/// 远程敌人组件：挂载在其他敌人的GameObject上，处理位置平滑插值
/// </summary>
public class RemoteEnemy : MonoBehaviour
{
    public int RoleId { get; private set; }
    public int ServerInstanceId { get; private set; }
    public Vector3 TargetPos;
    public Quaternion TargetRotation;
    public float smoothSpeed = 10f;
    private Transform _modelTransform;
    public EnemyCtrl Ctrl { get; private set; }

    public void Init(int roleId, int serverInstanceId, Vector3 pos)
    {
        RoleId = roleId;
        ServerInstanceId = serverInstanceId;
        TargetPos = pos;
        TargetRotation = Quaternion.identity;
        Ctrl = GetComponent<EnemyCtrl>();

        // 找模型子节点（Character 预制体的 root/playerModel）
        _modelTransform = GetComponentInChildren<EnemyModel>().transform;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, TargetPos, smoothSpeed * Time.deltaTime);
        Transform rotTarget = _modelTransform ? _modelTransform : transform;
        rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, TargetRotation, smoothSpeed * Time.deltaTime);
    }
}