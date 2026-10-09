using DG.Tweening;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Enemy;
using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Player
{
    // 输入子系统：玩法按键的注册 / 注销与按键触发的动作
    public class PlayerInputHandler
    {
        private readonly PlayerCore _core;
        private readonly PlayerStateMachine _stateMachine;
        private readonly PlayerSkillCombo _skillCombo;
        private readonly PlayerCameraBinder _cameraBinder;
        private readonly CharacterController _characterController;
        private readonly Transform _rootTransform;

        private EnemyCtrl _enemy;

        #region 拼刀 / 闪避的运行时标记

        private bool _isPining;
        private float _lastShiftPressTime;
        private const float DoubleTapInterval = 0.3f;
        private bool _isShiftDoubleTap;

        #endregion

        public PlayerInputHandler(PlayerCore core, PlayerStateMachine stateMachine, PlayerSkillCombo skillCombo,
            PlayerCameraBinder cameraBinder, CharacterController characterController, Transform rootTransform)
        {
            _core = core;
            _stateMachine = stateMachine;
            _skillCombo = skillCombo;
            _cameraBinder = cameraBinder;
            _characterController = characterController;
            _rootTransform = rootTransform;
        }

        /// <summary>
        /// 注册玩法按键。全部走 InputManager —— 开面板时会被输入锁自动屏蔽，
        /// 不再需要在每个输入点判断是否聚焦了聊天框等（原先只能靠打补丁）。
        /// </summary>
        public void Register()
        {
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.E, TryPin);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.Alpha6, DebugHeal);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.LeftShift, HandleEvade);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.Alpha1, SwitchSkillNormal);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.Alpha2, SwitchSkillSecond);
            InputManager.Instance.RegisterGameplayKeyDown(KeyCode.R, SwitchSkillEx);
            InputManager.Instance.RegisterGameplayMouseDown(0, OnMouse0);
            InputManager.Instance.RegisterGameplayMouseDown(1, OnMouse1);
            InputManager.Instance.RegisterGameplayMouseDown(2, OnMouse2);
        }

        public void Unregister()
        {
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.E, TryPin);
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.Alpha6, DebugHeal);
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.LeftShift, HandleEvade);
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.Alpha1, SwitchSkillNormal);
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.Alpha2, SwitchSkillSecond);
            InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.R, SwitchSkillEx);
            InputManager.Instance.UnregisterGameplayMouseDown(0, OnMouse0);
            InputManager.Instance.UnregisterGameplayMouseDown(1, OnMouse1);
            InputManager.Instance.UnregisterGameplayMouseDown(2, OnMouse2);
        }

        #region 技能连招切换

        private void SwitchSkillNormal() => SwitchSkill(ComboSet.Normal);
        private void SwitchSkillSecond() => SwitchSkill(ComboSet.Second);
        private void SwitchSkillEx() => SwitchSkill(ComboSet.Ex);

        private void SwitchSkill(ComboSet set)
        {
            if (!_core.IsLocalPlayer) return;
            if (_skillCombo.GetSkillConfig(set) == null) return;
            if (_skillCombo.IsCurrent(set)) return;

            if (set == ComboSet.Ex)
            {
                _skillCombo.UpdateSkillConfig(ComboSet.Ex);
                _stateMachine.ChangeTo(PlayerStateType.Ex);
                return;
            }

            _skillCombo.UpdateSkillConfig(set);
        }

        private void OnMouse0()
        {
            if (!_core.IsLocalPlayer) return;
            // 处于重击连招时，左键先切回普攻表（切表动作）
            if (_skillCombo.IsCurrent(ComboSet.Heavy)) SwitchSkill(ComboSet.Normal);
            // 不在攻击状态则进入攻击；已在攻击中则只写缓冲，接段交给 AttackState 消费
            TryEnterAttack();
            _skillCombo.EnqueueAttackInput(AttackInput.Normal);
        }

        private void OnMouse1()
        {
            if (!_core.IsLocalPlayer) return;
            if (_skillCombo.GetSkillConfig(ComboSet.Heavy) == null) return;

            // 攻击中：只写缓冲，切重击表 + 段内衔接交给 AttackState 决策
            if (_stateMachine.CurrentState == PlayerStateType.Attack)
            {
                _skillCombo.EnqueueAttackInput(AttackInput.Heavy);
                return;
            }

            // 非攻击状态：右键直接切重击表并立即出手重击，不需要先切表再点第二次。
            // 起手直接打重击第一段，不再入队 Heavy
            _skillCombo.UpdateSkillConfig(ComboSet.Heavy);
            TryEnterAttack();
        }

        private void OnMouse2()
        {
            if (!_core.IsLocalPlayer) return;
            // 中键重击：只在攻击段内有意义，采集入缓冲，是否命中「重击入口段」由 AttackState 决策
            _skillCombo.EnqueueAttackInput(AttackInput.HeavyEntry);
        }

        // 非攻击状态时进入攻击状态（Idle / Move 的「攻击入口」统一收口到这里）
        private void TryEnterAttack()
        {
            if (_stateMachine.CurrentState != PlayerStateType.Attack)
                _stateMachine.ChangeTo(PlayerStateType.Attack);
        }

        #endregion

        #region 拼刀

        private void TryPin()
        {
            if (_isPining) return;

            _enemy = Object.FindAnyObjectByType<EnemyCtrl>();
            if (!_enemy || !_enemy.isStartPin || _enemy.isPinFinished) return;

            TeleportToEnemy();
            if (_enemy.isStartPinTip) _enemy.isStartPinTip.gameObject.SetActive(false);
            _isPining = true;
            DOVirtual.DelayedCall(1f, () =>
            {
                _isPining = false;
                _cameraBinder.PinCamera.gameObject.SetActive(false);
            });
            _enemy.OnStiff();
        }

        private void TeleportToEnemy()
        {
            if (!_enemy) return;
            Vector3 dirToPlayer = (_rootTransform.position - _enemy.transform.position).normalized;
            dirToPlayer.y = 0;
            Vector3 teleportPos = _enemy.transform.position + dirToPlayer * 1f;
            teleportPos.y = _rootTransform.position.y;
            _characterController.enabled = false;
            _rootTransform.position = teleportPos;
            _characterController.enabled = true;
            _cameraBinder.PinCamera.gameObject.SetActive(true);
            _rootTransform.LookAt(_enemy.transform);
            _skillCombo.UpdateSkillConfig(ComboSet.Second, true);
            _stateMachine.ChangeTo(PlayerStateType.Attack, true);
        }

        #endregion

        // 调试用回血
        private void DebugHeal()
        {
            _core.Health = Mathf.Clamp(_core.Health + 50, 50, _core.MaxHealth);
        }

        #region 闪避相关

        private void HandleEvade()
        {
            // 已在闪避中：再按 Shift 走连闪（重新播 Evade 动画），而不是重复切状态
            if (_stateMachine.CurrentState == PlayerStateType.Evade)
            {
                if (_stateMachine.Machine.CurrentState is PlayerEvadeState evade)
                    evade.RefreshEvade();
                return;
            }

            _isShiftDoubleTap = Time.time - _lastShiftPressTime < DoubleTapInterval;

            _stateMachine.ChangeTo(PlayerStateType.Evade);
            PlayerEvadeState state = (PlayerEvadeState)_stateMachine.Machine.CurrentState;
            state.SetEvade(_isShiftDoubleTap);
            _lastShiftPressTime = Time.time;
        }

        #endregion
    }
}
