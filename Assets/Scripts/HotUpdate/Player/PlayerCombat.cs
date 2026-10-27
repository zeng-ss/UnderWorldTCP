using System;
using HotUpdate.Data;
using HotUpdate.Enemy;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
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

        // 血量是否已由属性初始化过（进游戏首次拿到属性时补满血，之后交给服务端）
        private bool _hpInitialized;

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
        /// 顺便把血量上限与防御上报服务端 —— 敌人攻击的伤害由服务端结算，那边需要同一份数值。
        /// 注意：本方法不再重置血量。进游戏首次拿到属性时补满一次（与服务端的满血初始化对齐），
        /// 之后血量只由服务端的受击广播修改。
        /// </summary>
        public void ApplyPlayerData(PlayerValueData valueData)
        {
            if (valueData == null) return;

            _core.PlayerValueData = valueData;
            _core.MaxHealth = valueData.MaxHealthValue;

            if (!_hpInitialized)
            {
                _core.Health = _core.MaxHealth;
                _hpInitialized = true;
                RefreshHealthUI();
            }
            else if (_core.Health > _core.MaxHealth)
            {
                _core.Health = _core.MaxHealth;
                RefreshHealthUI();
            }

            AppContext.Proto.RequestPlayerStats(_core.MaxHealth, valueData.DefenseValue);
        }

        #endregion

        #region 受击（服务端权威）

        // 订阅服务端下发的受击广播。玩家血量只能由服务端结算，本地不做任何扣血。
        public void RegisterHurtListener()
        {
            AppContext.Proto.OnPlayerHurtReceived -= ApplyServerHurt;
            AppContext.Proto.OnPlayerHurtReceived += ApplyServerHurt;
        }

        public void UnregisterHurtListener()
        {
            AppContext.Proto.OnPlayerHurtReceived -= ApplyServerHurt;
        }

        /// <summary>
        /// 服务端结算结果落地：覆盖血量 → 受击表现 / 死亡。广播对场景内所有人都发，
        /// 但只有本人（本地玩家）需要更新血量与状态，别人的受击表现为「谁打谁」的动画同步负责。
        /// </summary>
        private void ApplyServerHurt(PlayerHurtNtf ntf)
        {
            if (ntf == null || !_core.IsLocalPlayer) return;

            bool alreadyDead = _stateMachine.CurrentState == PlayerStateType.Dead;

            if (ntf.MaxHp > 0f) _core.MaxHealth = ntf.MaxHp;
            _core.Health = ntf.CurrHp;

            if (!alreadyDead)
            {
                // 屏幕反馈的强度仍取本地配置（敌人 prefab 上的 HitData），伤害数值以服务端为准
                HitData hitData = GetAttackerHitData(ntf.EnemyInstanceId);
                _presentation.PlayHurtFeedback(ntf.Damage, hitData?.vignetteValue ?? 0f, hitData?.screenImpulseValue ?? 0f);

                // 敌人武器的命中表现（特效 / 音效）。服务端只下发「命中了 + 伤害」，
                // 命中点用本地玩家当前位置近似 —— 本地再没有武器碰撞盒参与判定。
                AppContext.Enemy.PlayHitFeedback(ntf.EnemyInstanceId, _gameObject.transform.position);
            }

            if (ntf.IsDead)
            {
                if (alreadyDead) return;
                _stateMachine.ChangeTo(PlayerStateType.Dead);
                _gameObject.tag = "Untagged";
                RefreshHealthUI();
                return;
            }

            if (alreadyDead) return;
            _stateMachine.ChangeTo(PlayerStateType.Hurt, true);
            RefreshHealthUI();
        }

        private PlayerProPanel _playerProPanel;
        // 血量变化后刷新 HUD 血条
        private void RefreshHealthUI()
        {
            _playerProPanel ??= AppContext.Ui.GetPanel<PlayerProPanel>();
            _playerProPanel?.UpdateHealthFill(_core.Health, _core.MaxHealth);
        }

        // 挨打时用它取表现参数（震屏 / 暗角）。敌人可能已经被打死并注销，取不到就用 0。
        private static HitData GetAttackerHitData(int enemyInstanceId)
        {
            return AppContext.Enemy.TryGet(enemyInstanceId, out EnemyCtrl enemy) ? enemy.hitData : null;
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

            // 攻击的目标已经倒下：不播命中特效、不发攻击请求
            EnemyCtrl enemy = ((Component)hurt).GetComponent<EnemyCtrl>();
            if (enemy is not null && enemy.IsDead) return;

            HitData hitData = attackData.hitData;
            _presentation.PlayHitEffects(hitData, _skillCombo.CurVFXIndex, hurtPos);
            hurt.OnHurt(hitData, hurtSource);

            // 发送攻击请求到服务端
            if (enemy is not null && enemy.serverInstanceId > 0)
            {
                bool isExAttack = _stateMachine.CurrentState == PlayerStateType.Ex;
                float baseDamage = isExAttack ? _core.PlayerValueData.ExAttackValue : _core.PlayerValueData.AttackValue;

                // 离线调试没有服务端，本地按同一套公式结算一次，再走服务端回包的那条落地路径
                if (AppContext.IsOffline)
                {
                    enemy.OnServerAttackResult(
                        BuildOfflineAttackResult(enemy, baseDamage, _core.PlayerValueData.BaoJiValue));
                    return;
                }

                AppContext.Proto.RequestPlayerAttack(AppContext.Session.RoleId, enemy.serverInstanceId, baseDamage,
                    _core.PlayerValueData.BaoJiValue, isExAttack, enemy.OnServerAttackResult);
            }
        }

        // 离线调试用的随机数（服务端那套是 EnemyMgr 自己的 Random，两边不共享）
        private static readonly System.Random OfflineRand = new System.Random();

        // 离线调试：与 Server/LoginServer/Ctrl/EnemyMgr.ProcessAttack 完全相同的伤害公式 ——
        // 基础伤害 + 暴击时额外 200~1500，暴击率取角色的暴击属性。
        private static PlayerAttackRet BuildOfflineAttackResult(EnemyCtrl enemy, float baseDamage, float baojiPercent)
        {
            bool isBaoji = OfflineRand.Next(1, 101) <= baojiPercent;
            float damage = baseDamage;
            if (isBaoji) damage += OfflineRand.Next(200, 1501);

            float currHp = enemy.networkHealth - damage;
            bool isDead = currHp <= 0f;
            if (isDead) currHp = 0f;

            return new PlayerAttackRet
            {
                CmdCode = CmdCode.Succeed,
                EnemyInstanceId = enemy.serverInstanceId,
                AttackerRoleId = AppContext.Session.RoleId,
                DamageDealt = damage,
                EnemyCurrHp = currHp,
                EnemyMaxHp = enemy.maxHealthValue,
                IsDead = isDead,
                IsBaoji = isBaoji,
            };
        }

        public void OnHurt(HitData hitData, ISkillOwner hurtSource)
        {
            // 空实现：玩家的血量由服务端结算（见 ApplyServerHurt），本地武器命中不再扣血。
            // 敌人武器的碰撞盒也不再参与判定，命中表现改由服务端 PlayerHurtNtf 广播触发。
        }
    }
}
