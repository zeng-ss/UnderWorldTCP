using DamageNumbersPro;
using DG.Tweening;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Enemy
{
    public class EnemyCtrl : MonoBehaviour, IHurt, ISkillOwner, IStateMachineOwner
    {
        private const float HurtAnimDuration = 0.8f;
        [HideInInspector] public int serverInstanceId;
        [HideInInspector] public bool isHostDriven;
        [Header("配置")] [SerializeField] private EnemyAnimConfig animConfig;

        #region 数据

        public HitData hitData;
        public EnemyModel enemyModel;

        public float networkHealth, maxHealthValue = 200f;

        [HideInInspector] public EnemyStateType currentState = EnemyStateType.Idle;
        [HideInInspector] public bool isCanPlayHurtAni = true;

        [SerializeField] private GameObject healthBar;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text healthText;
        public GameObject isStartPinTip;
        [SerializeField] private DamageNumber damageNumber;

        public bool IsStartPin { get; set; }
        public bool IsPinFinished { get; private set; }

        #endregion

        #region 运行时状态

        private string _enemyName = "敌人";
        private int _enemyConfigId;
        private bool _inited;

        private string _currentAnimName = "Idle";
        private float _animBlockUntil;
        private string _lastServerAnim = "Idle";
        private EnemyStateType _lastServerState = EnemyStateType.Idle;

        private EnemyMotor _motor;
        private EnemySync _sync;
        private EnemyView _view;
        private EnemyStateMachine _stateMachine;

        #endregion

        #region 生命周期

        private void Start()
        {
            if (!isHostDriven && _inited && !IsDead && EnemyManager.IsLocalHost) EnableHostMode();
            EnsureHelpers();

            enemyModel.Init(this);

            // 敌人武器：碰撞盒由宿主端开合，命中判定在 WeaponController 里
            foreach (WeaponController weapon in GetComponentsInChildren<WeaponController>(true))
            {
                weapon.SetupForEnemy(this);
            }

            // 位移吃动画根运动：必须开 applyRootMotion，否则 OnAnimatorMove 不会被调用
            enemyModel.Animator.applyRootMotion = true;
            enemyModel.SetRootMotionAction(OnRootMotion);

            StopDistance = Anim(_currentAnimName).attackRange;
            ChangeState(EnemyStateType.Idle);
            RefreshHealthUI();
        }

        private void Update()
        {
            // 房主转移：自己成了新宿主就接管，反之交还控制权
            if (!isHostDriven && _inited && !IsDead && EnemyManager.IsLocalHost) EnableHostMode();
            if (isHostDriven && !EnemyManager.IsLocalHost) DisableHostMode();

            _view.FaceCamera();
            _motor.TickGravity(Time.deltaTime);

            if (isHostDriven)
            {
                _sync.UpdateHostTarget(Time.deltaTime);
                _sync.Report(Time.deltaTime);
                return;
            }

            if (_animBlockUntil > 0f && Time.time >= _animBlockUntil)
            {
                _animBlockUntil = 0f;
                currentState = _lastServerState;
                PlayAnimation(_lastServerAnim);
            }

            _sync.Follow(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _stateMachine?.Stop();
            if (AppContext.IsAlive) AppContext.Enemy.Unregister(this);
        }

        private void EnsureHelpers()
        {
            if (_motor != null) return;

            _motor = new EnemyMotor(this);
            _sync = new EnemySync(this, _motor);
            _view = new EnemyView(this, healthBar, fillImage, healthText, damageNumber, hitData);
        }

        #endregion

        #region 生成 / 对外数据

        /// 服务端生成敌人后，由 EnemyManager 调用落地基础数据并向实例表登记
        public void InitFromServer(int instanceId, float maxHp, float currHp, Vector3 pos, string enemyName,
            int enemyConfigId)
        {
            EnsureHelpers();

            _inited = true;
            serverInstanceId = instanceId;
            _enemyConfigId = enemyConfigId;
            if (!string.IsNullOrEmpty(enemyName)) _enemyName = enemyName;

            maxHealthValue = maxHp;
            networkHealth = currHp;

            _motor.Teleport(pos);
            _sync.ResetBaseline(pos);

            AppContext.Enemy.Register(this);
            RefreshHealthUI();
        }

        public bool IsHost => isHostDriven;

        /// <summary>已经倒下：不再作为攻击目标、不再接收位移</summary>
        public bool IsDead => currentState == EnemyStateType.Dead;

        public Animator ModelAnimator => enemyModel != null ? enemyModel.Animator : null;

        public string CurrentAnimName => _currentAnimName;

        public EnemyStateType CurrentState => currentState;

        public bool HasTarget => _sync != null && _sync.HasTarget;

        public Vector3 TargetPosition => _sync != null ? _sync.TargetPosition : transform.position;

        public float DistanceToTarget => _sync != null ? _sync.DistanceToTarget : 0f;

        public float StopDistance { get; private set; } = 2.5f;

        public EnemyAnimConfig.Entry Anim(string animName)
            => animConfig is not null ? animConfig.Get(animName) : EnemyAnimConfig.Default(animName);

        // 是不是刚冲刺过来（攻击状态据此决定用 Attack05 收招）
        public bool ArrivedFromRun { get; set; }

        // 下一次可以出招的时间
        public float NextAttackTime { get; set; }

        // 受击动画是否允许播放（攻击 / 死亡期间关掉，避免打断）
        public bool HurtAnimAllowed
        {
            set => isCanPlayHurtAni = value;
        }

        public string PickHurtAnim() => _view.PickHurtAnim();

        private void RefreshHealthUI() => _view.RefreshHealth(networkHealth, maxHealthValue);

        #endregion

        #region 宿主状态机

        public void ChangeState(EnemyStateType state)
        {
            if (!isHostDriven || _stateMachine == null) return;

            currentState = state;
            _stateMachine.ChangeTo(state);
        }

        public void EnableHostMode()
        {
            if (isHostDriven) return;

            isHostDriven = true;
            _stateMachine = new EnemyStateMachine(this);
            _stateMachine.Init();
            ChangeState(EnemyStateType.Idle);
        }

        private void DisableHostMode()
        {
            if (!isHostDriven) return;

            _stateMachine?.Stop();
            _stateMachine = null;
            isHostDriven = false;
        }

        // 武器碰撞盒打中了谁（WeaponController 回调）：交给服务端扣血
        public void ReportHit(int targetRoleId)
        {
            if (targetRoleId <= 0 || IsDead) return;
            if (AppContext.IsOffline) return;

            AppContext.Proto.RequestEnemyAttack(AppContext.Session.RoleId, serverInstanceId, targetRoleId);
        }

        #endregion

        #region 服务端同步入口

        /// <summary>服务端权威位置（非宿主才吃）</summary>
        public void ApplyServerPosition(float x, float y, float z, float rotationY)
        {
            _sync?.ApplyPosition(x, y, z, rotationY);
        }

        /// <summary>服务端权威动作与状态（服务端只在变化时下发）</summary>
        public void ApplyServerAnim(string animationName, int state)
        {
            if (isHostDriven) return; // 宿主自己播动画
            if (string.IsNullOrEmpty(animationName)) return;

            _lastServerAnim = animationName;
            _lastServerState = (EnemyStateType)state;
            _sync?.SetLastAnim(animationName);

            // 已死亡后不再被任何动作覆盖
            if (currentState == EnemyStateType.Dead && _lastServerState != EnemyStateType.Dead) return;

            // 受击动画播放期间不接受切换
            if (Time.time < _animBlockUntil) return;

            currentState = _lastServerState;
            PlayAnimation(animationName);
        }

        /// <summary>动画根运动：只有配置里 useRootMotion 的动画才位移</summary>
        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            if (_motor == null || IsDead) return;
            if (!Anim(_currentAnimName).useRootMotion) return;

            deltaPos.y = 0f;
            if (deltaPos.sqrMagnitude <= 0f) return;

            _motor.Move(deltaPos);
        }

        public void PlayAnimation(string animationName, float fixedTransitionTime = 0.1f)
        {
            _currentAnimName = animationName;
            enemyModel.Animator.CrossFadeInFixedTime(animationName, fixedTransitionTime, 0, 0f);
        }

        #endregion

        #region 受击 / 死亡

        /// 服务端攻击结算回包（自己打的走回调，别人打的走广播）
        public void OnServerAttackResult(PlayerAttackRet ret)
        {
            if (ret == null) return;

            bool isMine = ret.AttackerRoleId == AppContext.Session.RoleId;

            if (ret.IsDead)
            {
                networkHealth = 0;
                RefreshHealthUI();
                OnEnemyDead(isMine);
                return;
            }

            networkHealth = ret.EnemyCurrHp;
            RefreshHealthUI();

            if (isCanPlayHurtAni)
            {
                if (isHostDriven)
                {
                    ChangeState(EnemyStateType.Hurt); // 状态机里播完自己回 Idle
                }
                else
                {
                    // 非宿主：播受击动画，并在窗口内屏蔽服务端下发的常规动作
                    _animBlockUntil = Time.time + HurtAnimDuration;
                    currentState = EnemyStateType.Hurt;
                    PlayAnimation(PickHurtAnim());
                }
            }

            _view.ShowDamage(ret.DamageDealt, ret.IsBaoji);
        }

        // advanceTask：只有真正打死它的玩家才推进任务，避免同屏其他客户端重复上报
        private void OnEnemyDead(bool advanceTask)
        {
            if (currentState == EnemyStateType.Dead) return;

            AppContext.Enemy.Unregister(this);
            currentState = EnemyStateType.Dead;
            isCanPlayHurtAni = false;
            _animBlockUntil = 0f;

            if (advanceTask)
            {
                AppContext.Task.AdvanceByType(TaskEvent.KillEnemy, _enemyConfigId.ToString());
                AppContext.Enemy.RequestKillReward(serverInstanceId, _enemyName);
            }

            // 尸体不再参与命中判定：CC + 胶囊 + 子物体上的武器碰撞盒全部关掉
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null) capsule.enabled = false;
            _motor?.Stop();
            foreach (Collider col in GetComponentsInChildren<Collider>(true)) col.enabled = false;
            tag = "Untagged";

            if (isHostDriven) ChangeState(EnemyStateType.Dead);
            else PlayAnimation("Dead");

            DOVirtual.DelayedCall(5f, () => Destroy(gameObject));
        }

        /// 敌人受击入口（IHurt）。血量只认服务端结算，这里不做本地扣血。
        public void OnHurt(HitData data, ISkillOwner hurtSource)
        {
        }

        #endregion

        #region 武器 / ISkillOwner

        // 玩家打中敌人时的表现（特效 + 音效），不扣血
        public void OnHit(IHurt hurt, Vector3 hurtPos) => PlayHitFeedback(hurtPos);

        public void PlayHitFeedback(Vector3 hurtPos) => _view?.PlayHitFeedback(hurtPos);

        // 敌人武器命中玩家的结果由服务端广播，这三个接口只为满足契约
        public void StartSkillHit(int weaponIndex)
        {
        }

        public void StopSkillHit(int weaponIndex)
        {
        }

        public void SkillCanSwitch()
        {
        }

        #endregion

        #region 拼刀

        // 被玩家拼刀：定身 0.5 秒
        public void OnStiff()
        {
            if (IsPinFinished) return;

            IsStartPin = false;
            IsPinFinished = true;
            if (isStartPinTip != null) isStartPinTip.SetActive(false);

            enemyModel.Animator.speed = 0f;
            DOVirtual.DelayedCall(0.5f, () =>
            {
                enemyModel.Animator.speed = 1f;
            });
        }

        #endregion
    }
}