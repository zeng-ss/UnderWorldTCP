using System.Collections.Generic;
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
    /// 敌人控制器（纯表现层）。
    /// 位置、朝向、状态与动作全部由服务端下发，本类只做两件事：
    /// ① 把 transform 平滑插值到服务端权威位置；② 播放服务端指定的动作。
    /// 受击与死亡表现由服务端下发的攻击结算触发。
    /// 敌人没有「归属玩家」——所有客户端看到的是同一只怪，按 serverInstanceId 定位。
    public class EnemyCtrl : MonoBehaviour, IHurt, ISkillOwner
    {
        // 服务端实例ID → 敌人。位置与动作同步都靠这张表找到目标。
        public static readonly Dictionary<int, EnemyCtrl> Instances = new();

        // 服务端分配的敌人实例ID
        [HideInInspector] public int serverInstanceId;

        #region 数据

        public HitData hitData;
        public EnemyModel enemyModel;
        public float networkHealth;
        public float maxHealthValue = 200f;

        // 服务端下发的状态（武器命中判定等表现分支会读）
        [HideInInspector] public EnemyStateType currentState = EnemyStateType.Idle;

        // 是否允许播受击动画（处决/拼刀期间会关掉）
        [HideInInspector] public bool isCanPlayHurtAni = true;

        public GameObject healthBar;
        public Image fillImage;
        public TMP_Text healthText;
        public GameObject isStartPinTip;

        // 拼刀：由动画事件 EnemyModel.StartPin 置位，被玩家拼掉后置 false
        [HideInInspector] public bool isStartPin;
        [HideInInspector] public bool isPinFinished;

        public DamageNumber damageNumber;

        private CapsuleCollider _capsuleCollider;
        private CharacterController _characterController;

        private float _gravity = -5f;
        private Vector3 _velocity;

        // 服务端权威变换（插值目标）
        private Vector3 _targetPos;
        private float _targetYaw;
        private bool _hasServerPos;
        private bool _inited;

        // 服务端最近一次下发的动作，受击动画播完后用它复位
        private string _lastServerAnim = "Idle";
        private EnemyStateType _lastServerState = EnemyStateType.Idle;

        // 受击动画保护窗口：窗口内忽略服务端的常规动作切换，避免打断受击
        private float _animBlockUntil;

        // 位置插值速度（越大越紧跟服务端）
        private const float PositionLerpSpeed = 12f;
        private const float RotationLerpSpeed = 12f;

        // 受击动画时长（与 HurtFront / HurtBack 大致对齐）
        private const float HurtAnimDuration = 0.8f;

        #endregion

        #region 生命周期

        private void Start()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _characterController = GetComponent<CharacterController>();
            enemyModel.Init(this);

            // 命中体积由 CharacterController 提供（tag = enemy），CapsuleCollider 不参与
            if (_capsuleCollider != null) _capsuleCollider.enabled = false;

            if (!_inited)
            {
                // 没走服务端初始化（例如编辑器里直接摆一只）时给一份默认值
                maxHealthValue = 200f;
                networkHealth = maxHealthValue;
                _targetPos = transform.position;
                _hasServerPos = true;
                PlayAnimation("Idle");
            }

            RefreshHealthUI();
        }

        private void Update()
        {
            KeepLookPlayer(healthBar);

            // 受击动画播完，复位到服务端最近下发的动作
            if (_animBlockUntil > 0f && Time.time >= _animBlockUntil)
            {
                _animBlockUntil = 0f;
                currentState = _lastServerState;
                PlayAnimation(_lastServerAnim);
            }

            FollowServerPosition();
            ApplyGravity();
        }

        private void OnDestroy()
        {
            if (Instances.TryGetValue(serverInstanceId, out EnemyCtrl c) && c == this)
            {
                Instances.Remove(serverInstanceId);
            }
        }

        #endregion

        #region 服务端同步入口

        /// 服务端生成敌人后，由 RemotePlayerManager 调用落地基础数据
        public void InitFromServer(int instanceId, float maxHp, float currHp, Vector3 pos)
        {
            _inited = true;
            serverInstanceId = instanceId;
            maxHealthValue = maxHp;
            networkHealth = currHp;
            _targetPos = pos;
            _hasServerPos = true;
            transform.position = pos;
            Instances[instanceId] = this;
            RefreshHealthUI();
        }

        /// 服务端权威位置与朝向（每帧按同步频率下发）
        public void ApplyServerPosition(float x, float y, float z, float rotationY)
        {
            _targetPos = new Vector3(x, y, z);
            _targetYaw = rotationY;
            _hasServerPos = true;
        }

        /// 服务端权威动作与状态。服务端只在变化时下发，所以这里不做去重。
        public void ApplyServerAnim(string animationName, int state)
        {
            if (string.IsNullOrEmpty(animationName)) return;

            _lastServerAnim = animationName;
            _lastServerState = (EnemyStateType)state;

            // 已死亡后不再被任何动作覆盖
            if (currentState == EnemyStateType.Dead && (EnemyStateType)state != EnemyStateType.Dead) return;

            // 受击动画播放期间不接受切换，否则受击表现会被自己的动作打断
            if (Time.time < _animBlockUntil) return;

            currentState = _lastServerState;
            PlayAnimation(animationName);
        }

        #endregion

        #region 表现（位置 / 动画）

        // XZ 以服务端为权威，Y 交给本地重力贴地（服务端不跑物理，也不该跑）
        private void FollowServerPosition()
        {
            if (!_hasServerPos) return;

            float k = 1f - Mathf.Exp(-PositionLerpSpeed * Time.deltaTime);
            Vector3 cur = transform.position;
            transform.position = new Vector3(
                Mathf.Lerp(cur.x, _targetPos.x, k),
                cur.y,
                Mathf.Lerp(cur.z, _targetPos.z, k));

            Transform model = enemyModel != null ? enemyModel.transform : null;
            if (model == null) return;

            float rk = 1f - Mathf.Exp(-RotationLerpSpeed * Time.deltaTime);
            model.rotation = Quaternion.Slerp(model.rotation, Quaternion.Euler(0f, _targetYaw, 0f), rk);
        }

        private void ApplyGravity()
        {
            if (_characterController == null || !_characterController.enabled) return;

            _characterController.Move(_velocity * Time.deltaTime);
            if (_characterController.isGrounded) _velocity.y = -2f;
            else _velocity.y += _gravity * Time.deltaTime;
        }

        public void PlayAnimation(string animationName, float fixedTransitionTime = 0.1f)
        {
            if (enemyModel == null || enemyModel.Animator == null) return;
            enemyModel.Animator.CrossFadeInFixedTime(animationName, fixedTransitionTime, 0, 0f);
        }

        private void RefreshHealthUI()
        {
            if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(networkHealth / Mathf.Max(1f, maxHealthValue));
            if (healthText != null) healthText.text = $"{networkHealth}/{maxHealthValue}";
        }

        private void KeepLookPlayer(GameObject obj)
        {
            if (!obj || !Camera.main) return;
            obj.transform.LookAt(Camera.main.transform.position);
            obj.transform.Rotate(0, 180, 0);
        }

        #endregion

        #region 受击 / 死亡（全部由服务端结算驱动）

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

            if (!isCanPlayHurtAni) return;

            // 播受击动画，并在受击窗口内屏蔽服务端的常规动作
            _animBlockUntil = Time.time + HurtAnimDuration;
            currentState = EnemyStateType.Hurt;
            PlayHurtAnimation();

            Vector3 hurtPos = transform.position + Vector3.up * 1.5f;
            if (damageNumber != null)
            {
                damageNumber.SetColor(ret.IsBaoji ? Color.red : Color.white);
                damageNumber.Spawn(hurtPos, ret.DamageDealt);
            }
        }

        // 按攻击者方位选前/后受击动画
        private void PlayHurtAnimation()
        {
            PlayerCtrl player = Object.FindAnyObjectByType<PlayerCtrl>();
            if (player == null)
            {
                PlayAnimation("HurtFront");
                return;
            }

            Vector3 toAttacker = player.transform.position - transform.position;
            toAttacker.y = 0f;
            if (toAttacker.sqrMagnitude < 0.0001f)
            {
                PlayAnimation("HurtFront");
                return;
            }

            float dot = Vector3.Dot(transform.forward, toAttacker.normalized);
            PlayAnimation(dot > 0f ? "HurtFront" : "HurtBack");
        }

        // advanceTask：只有真正打死它的玩家才推进「击败敌人」任务，避免同屏其他客户端重复上报
        private void OnEnemyDead(bool advanceTask)
        {
            if (currentState == EnemyStateType.Dead) return;

            Instances.Remove(serverInstanceId);
            currentState = EnemyStateType.Dead;
            isCanPlayHurtAni = false;
            _animBlockUntil = 0f;

            if (advanceTask) AppContext.Task.AdvanceByType(TaskType.击败第一个敌人);

            if (_capsuleCollider != null) _capsuleCollider.enabled = false;
            if (_characterController != null) _characterController.enabled = false;
            tag = "Untagged";

            PlayAnimation("Dead");
            DOVirtual.DelayedCall(5f, () => { if (this != null) Destroy(gameObject); });
        }

        /// 敌人受击入口（IHurt）。血量只认服务端结算，这里不做本地扣血。
        public void OnHurt(HitData data, ISkillOwner hurtSource)
        {
        }

        #endregion

        #region 武器（敌人打玩家，纯本地表现）

        public void OnHit(IHurt hurt, Vector3 hurtPos)
        {
            if (hitData == null) return;

            GameObject obj = Instantiate(hitData.hitPrefabs[0]);
            obj.transform.position = hurtPos;
            Destroy(obj, obj.GetComponent<ParticleSystem>().main.duration);
            AppContext.Sound.PlaySound(hitData.hitClip, hurtPos);
            hurt.OnHurt(hitData, this);
        }

        #endregion

        #region ISkillOwner

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
            if (isPinFinished) return;

            isStartPin = false;
            isPinFinished = true;
            if (isStartPinTip != null) isStartPinTip.SetActive(false);

            if (enemyModel != null && enemyModel.Animator != null) enemyModel.Animator.speed = 0f;
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (enemyModel != null && enemyModel.Animator != null) enemyModel.Animator.speed = 1f;
            });
        }

        #endregion
    }
}
