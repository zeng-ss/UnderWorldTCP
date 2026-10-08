using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

// 玩家控制器：只负责子系统的创建、装配与只读暴露，以及必须由宿主承担的接口实现。
public class PlayerCtrl : MonoBehaviour, IStateMachineOwner, ISkillOwner, IHurt
{
    // —— 自身组件 / 预制体序列化字段 ——
    public CharacterController CharacterController { get; private set; }
    public PlayerModel playerModel;
    public float rotationSpeed;
    public DamageNumber damageNumber;

    [Header("技能连招配置（按序号：0 普攻 / 1 第二套连招 / 2 重击 / 3 EX；拼刀与防御反击复用序号 1）")] [SerializeField]
    private List<SkillConfig> skillConfigList = new();

    // 跨子系统共享状态
    public PlayerCore Core { get; private set; }

    // 表现层：动画 / 音效 / VFX / 受击反馈
    public PlayerPresentation Presentation { get; private set; }

    // 战斗：命中 / 受伤 / 死亡 / 服务端属性
    public PlayerCombat Combat { get; private set; }

    // 相机绑定
    public PlayerCameraBinder CameraBinder { get; private set; }

    // 玩法输入注册与按键动作
    public PlayerInputHandler InputHandler { get; private set; }

    // 移动 / 重力 / 根运动驱动
    public PlayerLocomotion Locomotion { get; private set; }

    // 技能连招：配置切换、段位与起手
    public PlayerSkillCombo SkillCombo { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }

    private bool _isInit;

    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        Core = new PlayerCore();
        playerModel.Init(this);

        Presentation = GetComponent<PlayerPresentation>() ?? gameObject.AddComponent<PlayerPresentation>();
        Presentation.Init(playerModel, damageNumber, transform);
        Presentation.BindVfxOwner(particle => particle.Init(this));

        CameraBinder = new PlayerCameraBinder();
        StateMachine = new PlayerStateMachine(Core);
        StateMachine.Init(this);
        Locomotion = new PlayerLocomotion(CharacterController, StateMachine);
        SkillCombo = new PlayerSkillCombo(Core, Presentation, skillConfigList);
        Combat = new PlayerCombat(Core, StateMachine, Presentation, SkillCombo, gameObject);
        InputHandler =
            new PlayerInputHandler(Core, StateMachine, SkillCombo, CameraBinder, CharacterController, transform);
        SkillCombo.InitDefault();
    }

    private void Start()
    {
        if (!Core.IsLocalPlayer) return;

        Combat.InitializeFromServer(AppContext.Session.MainRoleInfo);
        CameraBinder.Bind(transform, playerModel.transform);
        Locomotion.Init();
        StateMachine.ChangeTo(PlayerStateType.Idle);
        _isInit = true;

        InputHandler.Register();
        Combat.RegisterDataListener();
        Combat.ApplyPlayerData(AppContext.PlayerData.Current.Value);
        AppContext.Proto.StartPositionSync(transform, AppContext.Session.RoleId, playerModel.transform);
    }

    private void Update()
    {
        if (!Core.IsLocalPlayer || !_isInit || Core.IsLock) return;
        Locomotion.Tick();
    }

    private void OnDestroy()
    {
        InputHandler?.Unregister();
        Combat?.UnregisterDataListener();
        if (AppContext.IsAlive) AppContext.Proto.StopPositionSync();
    }

    public void PlayAnimation(string animationName, float fixedTransitionTime = 0.1f)
    {
        Presentation.PlayAnimation(animationName, fixedTransitionTime);
        AppContext.Proto.RequestSyncAni(AppContext.Session.RoleId, animationName, SkillCombo.CurSkillIndex);
    }

    // 接口实现

    public void StartSkillHit(int weaponIndex) => SkillCombo.StartSkillHit();

    public void StopSkillHit(int weaponIndex) => SkillCombo.StopSkillHit();

    public void SkillCanSwitch() => SkillCombo.SkillCanSwitch();

    public void OnHit(IHurt hurt, Vector3 hurtPos) => Combat.OnHit(hurt, hurtPos, this);

    public void OnHurt(HitData hitData, ISkillOwner hurtSource) => Combat.OnHurt(hitData, hurtSource);
}