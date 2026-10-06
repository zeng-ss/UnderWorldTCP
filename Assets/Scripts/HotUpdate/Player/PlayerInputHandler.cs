using DG.Tweening;
using UnityEngine;

/// <summary>
/// 输入子系统：玩法按键的注册 / 注销与按键触发的动作
/// （技能切换、鼠标连招、闪避、拼刀、调试回血）。
/// 依赖均为单向：共享状态、状态机、连招配置、相机绑定，加上两个组件引用。
/// 注意：注册 / 注销必须用方法组，不能用 lambda —— lambda 每次生成的委托不相等，注销不掉。
/// </summary>
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
        InputManager.Instance.RegisterGameplayKeyDown(KeyCode.Alpha1, SwitchSkill0);
        InputManager.Instance.RegisterGameplayKeyDown(KeyCode.Alpha2, SwitchSkill1);
        InputManager.Instance.RegisterGameplayKeyDown(KeyCode.R, SwitchSkill3);
        InputManager.Instance.RegisterGameplayMouseDown(0, OnMouse0);
        InputManager.Instance.RegisterGameplayMouseDown(1, OnMouse1);
    }

    public void Unregister()
    {
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.E, TryPin);
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.Alpha6, DebugHeal);
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.LeftShift, HandleEvade);
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.Alpha1, SwitchSkill0);
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.Alpha2, SwitchSkill1);
        InputManager.Instance.UnregisterGameplayKeyDown(KeyCode.R, SwitchSkill3);
        InputManager.Instance.UnregisterGameplayMouseDown(0, OnMouse0);
        InputManager.Instance.UnregisterGameplayMouseDown(1, OnMouse1);
    }

    #region 技能连招切换

    private void SwitchSkill0() => SwitchSkillByInput(0);
    private void SwitchSkill1() => SwitchSkillByInput(1);
    private void SwitchSkill3() => SwitchSkillByInput(3);

    private void SwitchSkillByInput(int index)
    {
        if (!_core.IsLocalPlayer) return;
        if (index < 0 || index >= _skillCombo.SkillConfigList.Count) return;
        if (_skillCombo.CurSkillConfig == _skillCombo.SkillConfigList[index]) return;

        // 大招（EX）走独立状态，不只是换连招配置
        if (index == 3)
        {
            _skillCombo.UpdateSkillConfig(3);
            _stateMachine.ChangeTo(PlayerStateType.Ex);
            return;
        }

        _skillCombo.UpdateSkillConfig(index);
    }

    private void OnMouse0()
    {
        if (!_core.IsLocalPlayer) return;
        // 处于第三套连招时，左键切回第一套
        if (_skillCombo.SkillConfigList.Count > 2 && _skillCombo.CurSkillConfig == _skillCombo.SkillConfigList[2])
            SwitchSkillByInput(0);
    }

    private void OnMouse1()
    {
        if (!_core.IsLocalPlayer) return;
        if (_skillCombo.SkillConfigList.Count > 2 && _skillCombo.CurSkillConfig != _skillCombo.SkillConfigList[2])
            _skillCombo.UpdateSkillConfig(2);
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
        _skillCombo.UpdateSkillConfig(1, true);
        _stateMachine.ChangeTo(PlayerStateType.Attack, true);
    }

    #endregion

    /// <summary>调试用回血</summary>
    private void DebugHeal()
    {
        _core.Health = Mathf.Clamp(_core.Health + 50, 50, _core.MaxHealth);
    }

    #region 闪避相关

    private void HandleEvade()
    {
        _isShiftDoubleTap = Time.time - _lastShiftPressTime < DoubleTapInterval;

        _stateMachine.ChangeTo(PlayerStateType.Evade);
        PlayerEvadeState state = (PlayerEvadeState)_stateMachine.Machine.CurrentState;
        state.SetEvade(_isShiftDoubleTap);
        _lastShiftPressTime = Time.time;
    }

    #endregion
}