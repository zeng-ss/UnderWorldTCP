using System.Collections.Generic;
using System.Linq;
using HotUpdate.Enemy;
using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Player
{
    public class WeaponController : MonoBehaviour
    {
        private BoxCollider _collider;
        [Header("玩家")] [SerializeField] private PlayerCtrl player;
        [Header("敌人")] [SerializeField] private EnemyCtrl enemy;
        [Header("敌人标签列表")] [SerializeField] private List<string> targetTagList = new();
        private List<IHurt> _enemyList = new();

        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.enabled = false;
        }

        public void StartSkillHit()
        {
            _collider.enabled = true;
        }

        public void StopSkillHit()
        {
            _collider.enabled = false;
            _enemyList.Clear();
        }

        private void OnTriggerStay(Collider other)
        {
            // 只处理本地玩家的武器碰撞
            if (player != null)
            {
                if (!player.Core.IsLocalPlayer) return;
                if (!targetTagList.Contains(other.tag)) return;
                IHurt target = other.GetComponent<IHurt>();
                if (_enemyList.Contains(target)) return;
                player.OnHit(target, other.ClosestPoint(transform.position));
                _enemyList.Add(target);
                if (player.StateMachine.CurrentState == PlayerStateType.Ex) return;
                // 仅在非顿帧时触发顿帧
                var attackState = (PlayerAttackState)player.StateMachine.Machine.CurrentState;
                if (attackState == null) return;
                if (!attackState.IsInHitStop) attackState.EnterHitStop(); // 使用默认配置（0.08s顿帧，完全暂停）
            }
            // 敌人只处理服务器的武器碰撞
            else
            {
                if (!enemy.IsServer) return;
                if (!targetTagList.Contains(other.tag) || enemy.currentState != EnemyStateType.Attack) return;
                IHurt target = other.GetComponent<IHurt>();
                if (_enemyList.Contains(target)) return;
                enemy.OnHit(target, other.ClosestPoint(transform.position));
                _enemyList.Add(target);
            }
        }

    }
}
