using UnityEngine;

/// <summary>
/// 移动子系统：重力、CharacterController 位移与着地判断（原 PlayerCtrl.Update 的重力块）。
/// PlayerCtrl.Update 只需调 Tick()。
/// </summary>
public class PlayerLocomotion
{
    private const float Gravity = -5f;

    private readonly CharacterController _characterController;
    private readonly PlayerStateMachine _stateMachine;
    private Vector3 _velocity;
    private bool hasGravity;

    public bool IsOnGround { get; private set; }

    public PlayerLocomotion(CharacterController characterController, PlayerStateMachine stateMachine)
    {
        _characterController = characterController;
        _stateMachine = stateMachine;
    }

    /// <summary>原 PlayerCtrl.Init() 里与移动相关的部分</summary>
    public void Init()
    {
        _characterController.enabled = true;
        hasGravity = true;
    }

    /// <summary>逐帧调用，内部逻辑与重构前完全一致</summary>
    public void Tick()
    {
        if (!_characterController.enabled && _stateMachine.CurrentState != PlayerStateType.Dead)
            _characterController.enabled = true;

        if (!hasGravity || !_characterController.enabled) return;

        _characterController.Move(_velocity * Time.deltaTime);
        IsOnGround = _characterController.isGrounded;
        if (IsOnGround)
        {
            _velocity.y = -2f;
        }
        else
        {
            _velocity.y += Gravity * Time.deltaTime;
        }
    }
}
