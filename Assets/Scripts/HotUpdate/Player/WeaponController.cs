using System.Collections.Generic;
using System.Linq;
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
            _hitRoleIds.Clear();

            if (player == null)
            {
                // 敌人武器：只有宿主端需要开碰撞盒做命中判定，其他人只看动画
                if (_enemyOwner != null && _enemyOwner.IsHost) _collider.enabled = true;
                return;
            }

            _collider.enabled = true;
        }

        public void StopSkillHit()
        {
            _collider.enabled = false;
            _enemyList.Clear();
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
            if (_enemyList.Contains(target)) return;
            player.OnHit(target, other.ClosestPoint(transform.position));
            _enemyList.Add(target);
            if (player.StateMachine.CurrentState == PlayerStateType.Ex) return;
            // 仅在非顿帧时触发顿帧
            var attackState = (PlayerAttackState)player.StateMachine.Machine.CurrentState;
            if (attackState == null) return;
            if (!attackState.IsInHitStop) attackState.EnterHitStop(); // 使用默认配置（0.08s顿帧，完全暂停）
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