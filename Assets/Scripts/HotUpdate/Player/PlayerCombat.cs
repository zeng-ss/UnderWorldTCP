using System;
using HotUpdate.Data;
using HotUpdate.Enemy;
using HotUpdate.Event;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Player
{
    // 战斗子系统：受击 / 死亡 / 命中判定发起、血量与角色属性管理
    public class PlayerCombat
    {
        private readonly PlayerCore _core;
        private readonly PlayerStateMachine _stateMachine;
        private readonly PlayerPresentation _presentation;
        private readonly PlayerSkillCombo _skillCombo;
        private readonly GameObject _gameObject;

        public PlayerCombat(PlayerCore core, PlayerStateMachine stateMachine, PlayerPresentation presentation,
            PlayerSkillCombo skillCombo, GameObject gameObject)
        {
            _core = core;
            _stateMachine = stateMachine;
            _presentation = presentation;
            _skillCombo = skillCombo;
            _gameObject = gameObject;
        }

        /// <summary>
        /// Start 阶段调用：优先使用从服务器加载的角色数据，没有则回退到默认面板血量
        /// </summary>
        public void InitializeFromServer(MainRoleInfo info)
        {
            if (info != null)
            {
                _core.PlayerValueData = new PlayerValueData
                {
                    ID = (ulong)info.BaseInfo.RoleId,
                };
            }
            else
            {
                _core.Health = _core.PlayerValueData.MaxHealthValue;
                _core.MaxHealth = _core.PlayerValueData.MaxHealthValue;
            }
        }

        #region 属性同步

        public void RegisterDataListener()
        {
            AppContext.PlayerData.Current.OnValueChanged -= ApplyPlayerData;
            AppContext.PlayerData.Current.OnValueChanged += ApplyPlayerData;
        }

        public void UnregisterDataListener()
        {
            AppContext.PlayerData.Current.OnValueChanged -= ApplyPlayerData;
        }

        /// <summary>
        /// 属性重算完成后的同步。统一以 PlayerDataService.Current 为唯一数据源。
        /// </summary>
        public void ApplyPlayerData(PlayerValueData valueData)
        {
            if (valueData == null) return;

            _core.PlayerValueData = valueData;
            _core.MaxHealth = valueData.MaxHealthValue;
            if (_core.Health <= 0 || _core.Health > _core.MaxHealth) _core.Health = _core.MaxHealth;
        }

        #endregion

        /// <summary>
        /// 命中判定。hurtSource
        /// </summary>
        public void OnHit(IHurt hurt, Vector3 hurtPos, ISkillOwner hurtSource)
        {
            if (!_core.IsLocalPlayer) return;

            if (_skillCombo.CurAttackIndex == -1) _skillCombo.CurAttackIndex = 0;
            AttackData attackData = _skillCombo.CurrentAttackData;
            if (attackData == null) return;
            HitData hitData = attackData.hitData;
            _presentation.PlayHitEffects(hitData, _skillCombo.CurVFXIndex, hurtPos);
            hurt.OnHurt(hitData, hurtSource);

            // 发送攻击请求到服务端
            EnemyCtrl enemy = ((Component)hurt).GetComponent<EnemyCtrl>();
            if (enemy is not null && enemy.serverInstanceId > 0)
            {
                bool isExAttack = _stateMachine.CurrentState == PlayerStateType.Ex;
                float baseDamage = isExAttack ? _core.PlayerValueData.ExAttackValue : _core.PlayerValueData.AttackValue;
                AppContext.Proto.RequestPlayerAttack(AppContext.Session.RoleId, enemy.serverInstanceId, baseDamage,
                    _core.PlayerValueData.BaoJiValue, isExAttack, enemy.OnServerAttackResult);
            }
        }

        public void OnHurt(HitData hitData, ISkillOwner hurtSource)
        {
            _core.Health -= Math.Max(0,
                hitData.damageValue - (float)Math.Round(_core.PlayerValueData.DefenseValue / 10, 2));
            if (_core.Health <= 0)
            {
                _stateMachine.ChangeTo(PlayerStateType.Dead);
                _gameObject.tag = "Untagged";
                return;
            }

            _stateMachine.ChangeTo(PlayerStateType.Hurt, true);
            _presentation.PlayHurtFeedback(hitData.damageValue, hitData.vignetteValue, hitData.screenImpulseValue);
        }
    }
}
