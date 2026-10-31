using System.Collections.Generic;
using System.Linq;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.Enemy;
using HotUpdate.Network;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Player
{
    public class WeaponController : MonoBehaviour
    {
        private BoxCollider _collider;

        [Header("玩家（敌人预制体上此字段为空）")] [SerializeField]
        private PlayerCtrl player;

        [Header("命中标签列表")] [SerializeField] private List<string> targetTagList = new();
        private List<IHurt> _enemyList = new();

        // 同一次挥砍内的重复命中：记录每个目标上次被打的时间与本次挥砍已命中次数
        // 直接用目标对象当 key（引用相等），不用 GetInstanceID —— 那个在 Unity 6 已经过时
        private readonly Dictionary<IHurt, float> _lastHitTime = new();
        private int _hitsThisSwing;

        // 配了多段时，动画事件的窗口可能太短（几帧），这里兜底把碰撞盒多撑一会儿
        private float _rehitCloseAt;
        private bool _pendingClose;

        // 敌人武器：宿主端用碰撞盒做命中判定，一次挥招每个目标只报一次
        private EnemyCtrl _enemyOwner;
        private readonly List<int> _hitRoleIds = new();

        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.enabled = false;
        }

        /// <summary>敌人武器初始化：由 EnemyCtrl 在自己 Start 时调（player 为空即敌人武器）</summary>
        public void SetupForEnemy(EnemyCtrl enemy)
        {
            _enemyOwner = enemy;
        }

        public void StartSkillHit()
        {
            _enemyList.Clear();
            _lastHitTime.Clear();
            _hitsThisSwing = 0;
            _hitRoleIds.Clear();

            if (player == null)
            {
                // 敌人武器：只有宿主端需要开碰撞盒做命中判定，其他人只看动画
                if (_enemyOwner != null && _enemyOwner.IsHost) _collider.enabled = true;
                return;
            }

            _collider.enabled = true;

            // 配了多段：按「几段 × 间隔」算一个最短窗口，事件窗口比它短也不会少打
            _pendingClose = false;
            AttackData data = player.SkillCombo.CurrentAttackData;
            if (data != null && data.rehitInterval > 0f && data.maxHitsPerSwing > 1)
            {
                _rehitCloseAt = Time.time + (data.maxHitsPerSwing - 1) * data.rehitInterval + 0.05f;
            }
            else
            {
                _rehitCloseAt = 0f;
            }
        }

        private void Update()
        {
            if (!_pendingClose) return;
            if (Time.time < _rehitCloseAt) return;

            _pendingClose = false;
            _collider.enabled = false;
            _enemyList.Clear();
            _lastHitTime.Clear();
            _hitsThisSwing = 0;
        }

        public void StopSkillHit()
        {
            // 配了多段且窗口不够长：先不关，等 Update 到点再关
            if (_rehitCloseAt > Time.time)
            {
                _pendingClose = true;
                return;
            }

            _collider.enabled = false;
            _enemyList.Clear();
            _lastHitTime.Clear();
            _hitsThisSwing = 0;
            _hitRoleIds.Clear();
        }

        private void OnTriggerStay(Collider other)
        {
            // 敌人武器：命中判定就在碰撞盒里做（宿主端），命中谁上报谁
            if (player == null)
            {
                HandleEnemyHit(other);
                return;
            }

            if (!player.Core.IsLocalPlayer) return;
            if (!targetTagList.Contains(other.tag)) return;
            IHurt target = other.GetComponent<IHurt>();
            if (target == null) return;
            // 已经倒下的敌人不再算目标（哪怕碰撞体残留）
            if (target is HotUpdate.Enemy.EnemyCtrl dead && dead.IsDead) return;
            if (!CanHitAgain(target)) return;

            player.OnHit(target, other.ClosestPoint(transform.position));
            if (player.StateMachine.CurrentState == PlayerStateType.Ex) return;
            // 仅在非顿帧时触发顿帧；开关与时长由这一刀的配置决定（在 PlayerAttackState 内部解析）
            var attackState = (PlayerAttackState)player.StateMachine.Machine.CurrentState;
            if (attackState == null || attackState.IsInHitStop) return;
            attackState.EnterHitStop(target);
        }

        // 一次挥砍内能否再次命中这个目标：
        //   · rehitInterval = 0 → 旧行为，一次挥砍每个目标只打一下
        //   · rehitInterval > 0 → 碰撞盒开着期间按间隔重复命中（一次挥砍打出多段）
        private bool CanHitAgain(IHurt target)
        {
            AttackData data = player.SkillCombo.CurrentAttackData;
            int maxHits = data?.maxHitsPerSwing ?? 0;
            if (maxHits > 0 && _hitsThisSwing >= maxHits) return false;

            float interval = data?.rehitInterval ?? 0f;

            if (interval <= 0f)
            {
                if (_enemyList.Contains(target)) return false;
                _enemyList.Add(target);
            }
            else
            {
                // Time.time 会随顿帧暂停，间隔按「实际战斗时间」算
                if (_lastHitTime.TryGetValue(target, out float last) && Time.time - last < interval) return false;
                _lastHitTime[target] = Time.time;
            }

            _hitsThisSwing++;
            return true;
        }

        // 敌人武器命中：碰撞盒碰到谁就上报谁，服务端只负责扣血
        private void HandleEnemyHit(Collider other)
        {
            if (_enemyOwner == null || !_enemyOwner.IsHost || _enemyOwner.IsDead) return;

            PlayerCtrl target = other.GetComponentInParent<PlayerCtrl>();
            if (target == null || target.Core == null || target.Core.Health <= 0f) return;

            int roleId;
            RemotePlayer remote = target.GetComponent<RemotePlayer>();
            if (remote != null)
            {
                roleId = remote.RoleId;
            }
            else
            {
                if (!target.Core.IsLocalPlayer) return;
                roleId = AppContext.Session.RoleId;
            }

            if (roleId <= 0 || _hitRoleIds.Contains(roleId)) return;

            _hitRoleIds.Add(roleId);
            _enemyOwner.ReportHit(roleId);
        }
    }
}