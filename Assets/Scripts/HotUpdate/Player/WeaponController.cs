using System.Collections.Generic;
using System.Linq;
using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Player
{
    public class WeaponController : MonoBehaviour
    {
        private BoxCollider _collider;

        [Header("玩家（敌人预制体上此字段为空）")] [SerializeField]
        private PlayerCtrl player;

        [Header("命中标签列表")] [SerializeField] private List<string> targetTagList = new();
        private List<IHurt> _enemyList = new();

        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.enabled = false;
        }

        // 敌人武器的命中判定已移到服务端（EnemyMgr 出招后按距离结算并广播 PlayerHurtNtf），
        // player 为空即敌人武器，动画事件照旧会调到这里，但不再开合碰撞盒。
        public void StartSkillHit()
        {
            if (player == null) return;
            _collider.enabled = true;
        }

        public void StopSkillHit()
        {
            _collider.enabled = false;
            _enemyList.Clear();
        }

        private void OnTriggerStay(Collider other)
        {
            // 只处理本地玩家的武器碰撞；敌人武器的命中表现由服务端受击广播驱动（见 EnemyCtrl.OnHit）
            if (player == null) return;
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
    }
}