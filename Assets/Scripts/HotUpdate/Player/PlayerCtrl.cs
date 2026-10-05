using System.Collections.Generic;
using DamageNumbersPro;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 玩家控制器：只做子系统装配与对外 API 转发。
/// 依赖严格单向：PlayerCtrl → 子系统 → PlayerCore（共享状态），
/// 子系统之间只注入自己依赖的具体类型，互不引用、也不引用本类。
/// </summary>
public class PlayerCtrl : MonoBehaviour, IState_MachineOwner, ISkillOwner, IHurt
{
    // —— 共享状态（外部访问或预制体序列化需要）——
    public CharacterController CharacterController { get; private set; }
    public Transform CameraTransform => _cameraBinder.CameraTransform;
    public PlayerModel playerModel;
    public float rotationSpeed;

    public bool IsLock
    {
        get => _core.IsLock;
        set => _core.IsLock = value;
    }

    public DamageNumber damageNumber;
    public CinemachineVirtualCamera VirtualCameraEx => _cameraBinder.VirtualCameraEx;

    public float Health
    {
        get => _core.Health;
        set => _core.Health = value;
    }

    public float MaxHealth
    {
        get => _core.MaxHealth;
        set => _core.MaxHealth = value;
    }

    public bool IsLocalPlayer
    {
        get => _core.IsLocalPlayer;
        set => _core.IsLocalPlayer = value;
    }

    // —— 子系统 ——
    private PlayerPresentation _presentation;
    private PlayerNetworkSync _network;
    private PlayerCombat _combat;
    private PlayerCameraBinder _cameraBinder;
    private PlayerCore _core;
    private PlayerInputHandler _input;
    private PlayerLocomotion _locomotion;
    private PlayerSkillCombo _skillCombo;
    private PlayerStateMachine _stateMachine;
    private bool _isInit;

    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        playerModel.Init(this);

        _core = new PlayerCore();
        _presentation = GetComponent<PlayerPresentation>() ?? gameObject.AddComponent<PlayerPresentation>();
        _presentation.Init(playerModel, damageNumber, transform);
        _presentation.BindVfxOwner(particle => particle.Init(this));

        _network = new PlayerNetworkSync();
        _cameraBinder = new PlayerCameraBinder();
        _stateMachine = new PlayerStateMachine(_core);
        _stateMachine.Init(this);
        _locomotion = new PlayerLocomotion(CharacterController, _stateMachine);
        _skillCombo = new PlayerSkillCombo(_core, _presentation, _network);
        _combat = new PlayerCombat(_core, _stateMachine, _presentation, _network, _skillCombo, gameObject);
        _input = new PlayerInputHandler(_core, _stateMachine, _skillCombo, _cameraBinder, CharacterController,
            transform);
        _skillCombo.InitDefault();
    }

    private void Start()
    {
        if (!IsLocalPlayer) return;

        _combat.InitializeFromServer(AppContext.Session.MainRoleInfo);
        _cameraBinder.Bind(transform, playerModel.transform);
        _locomotion.Init();
        ChangeState(PlayerStateType.Idle);
        _isInit = true;

        _input.Register();
        _combat.RegisterDataListener();
        _combat.ApplyPlayerData(AppContext.PlayerData.Current.Value);
        _network.StartPositionSync(transform, playerModel.transform);
    }

    private void Update()
    {
        if (!IsLocalPlayer || !_isInit || IsLock) return;
        _locomotion.Tick();
    }

    private void OnDestroy()
    {
        _input?.Unregister();
        _combat?.UnregisterDataListener();
        _network?.StopPositionSync();
    }

    // —— 状态机转发 ——
    public PlayerStateType CurrentState => _stateMachine.CurrentState;
    public PlayerStateType LastState => _stateMachine.LastState;
    public State_Machine StateMachine => _stateMachine.Machine;

    public void ChangeState(PlayerStateType stateType, bool isResfeshState = false) =>
        _stateMachine.ChangeTo(stateType, isResfeshState);

    // —— 技能连招转发 ——
    public int CurAttackIndex
    {
        get => _skillCombo.CurAttackIndex;
        set => _skillCombo.CurAttackIndex = value;
    }

    public int CurVFXIndex => _skillCombo.CurVFXIndex;
    public SkillConfig CurSkillConfig => _skillCombo.CurSkillConfig;
    public List<SkillConfig> SkillConfigList => _skillCombo.SkillConfigList;
    public bool CanSwitchSkill => _skillCombo.CanSwitchSkill;

    public void UpdateSkillConfig(int skillIndex, bool isPin = false) =>
        _skillCombo.UpdateSkillConfig(skillIndex, isPin);

    public void StartSkill(AttackData attackData) => _skillCombo.StartSkill(attackData);
    public void StartSkillHit(int weaponIndex) => _skillCombo.StartSkillHit(weaponIndex);
    public void StopSkillHit(int weaponIndex) => _skillCombo.StopSkillHit(weaponIndex);
    public void SkillCanSwitch() => _skillCombo.SkillCanSwitch();

    public void SpawnRemoteVfx(int skillConfigIndex, int attackIndex, int vfxIndex) =>
        _skillCombo.SpawnRemoteVfx(skillConfigIndex, attackIndex, vfxIndex);

    // —— 战斗 / 血量转发 ——
    public void OnHit(IHurt hurt, Vector3 hurtPos) => _combat.OnHit(hurt, hurtPos, this);
    public void OnHurt(HitData hitData, ISkillOwner hurtSource) => _combat.OnHurt(hitData, hurtSource);
    public float GetHealth() => Health;
    public float GetMaxHealth() => MaxHealth;

    // —— 表现 + 联网转发 ——
    /// <summary>播放动画并同步给服务端</summary>
    public void PlayAnimation(string animationName, float fixedTransitionTime = 0.1f)
    {
        _presentation.PlayAnimation(animationName, fixedTransitionTime);
        _network.SyncAnimation(animationName, _skillCombo.CurSkillIndex);
    }
}