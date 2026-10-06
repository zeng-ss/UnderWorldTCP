using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMoveState : PlayerState
{
    private enum MoveState
    {
        RunStart,   // 起步
        Run,        // 奔跑中
        RunEnd,     // 急停
        RunStartEnd,// 起步立即停止
        TurnBack    // 转身
    }
    
    private MoveState _currentMoveState;
    private bool _hasMovementInput;
    private float _inputMagnitude;
    private Vector3 _currentMoveDir;
    private Vector3 _lastMoveDirection;
    
    public override void Enter()
    {
        if (!Player.IsLocalPlayer) return;
        _lastMoveDirection = Vector3.zero;
        CheckInput();
        if (_hasMovementInput)
        {
            TransitionToState(MoveState.RunStart);
        }
        Player.playerModel.SetRootMotionAction(OnRootMation);
    }

    private void OnRootMation(Vector3 arg1, Quaternion arg2) { Player.CharacterController.Move(arg1); }

    public override void Update()
    {
        if (!Player.IsLocalPlayer) return;
        // 任何独占型面板打开时都锁住玩法输入，状态内部不再需要各自判断
        if (!InputManager.Instance.IsGameplayInputEnabled) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            Player.ChangeState(PlayerStateType.Idle);
            return;
        }
        CheckInput();
        HandleMovementTransitions();
        if (Input.GetKeyDown(KeyCode.Mouse0) || 
            (Input.GetKeyDown(KeyCode.Mouse1) && Player.CurSkillConfig == Player.SkillConfigList[2]))
        {
            Player.ChangeState(PlayerStateType.Attack);
        }
    }
    
    private void CheckInput()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        
        _inputMagnitude = new Vector2(horizontal, vertical).magnitude;
        _hasMovementInput = _inputMagnitude > 0.1f;
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Player.ChangeState(PlayerStateType.Evade);
            return;
        }
        // 计算当前移动方向并处理旋转（所有状态都旋转）
        _currentMoveDir = HandleRotation(horizontal, vertical);
        
        // 转身检测 - 只在奔跑状态进行，起步状态不检测转身
        if (_currentMoveState == MoveState.Run && 
            _hasMovementInput && 
            _lastMoveDirection.magnitude > 0.1f)
        {
            float angle = Vector3.Angle(_lastMoveDirection, _currentMoveDir);
            if (angle > 120f)
            {
                TransitionToState(MoveState.TurnBack);
            }
        }
        
        // 更新上一帧的移动方向
        if (_currentMoveDir.magnitude > 0.1f)
        {
            _lastMoveDirection = new Vector3(_currentMoveDir.x, 0, _currentMoveDir.z).normalized;
        }
    }
    
    // 处理旋转 - 所有状态都使用平滑旋转
    private Vector3 HandleRotation(float h, float v) 
    {
        Transform camTransform = Player.CameraTransform?.transform;
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        
        Vector3 moveDir = (camForward * v + camRight * h).normalized;
        
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            
            // 转身状态直接瞬间转向，其他状态平滑旋转
            if (_currentMoveState == MoveState.TurnBack)
            {
                Player.playerModel.transform.rotation = targetRotation;
            }
            else
            {
                Player.playerModel.transform.rotation = Quaternion.Slerp(
                    Player.playerModel.transform.rotation, 
                    targetRotation,
                    Player.rotationSpeed * Time.deltaTime
                );
            }
        }
        
        return moveDir;
    }
    
    private void HandleMovementTransitions()
    {
        switch (_currentMoveState)
        {
            case MoveState.RunStart:
                if (!_hasMovementInput)
                {
                    TransitionToState(MoveState.RunStartEnd);
                }
                else if (IsAnimationFinished("Run_Start"))
                {
                    // 起步动画完成，进入奔跑状态
                    TransitionToState(MoveState.Run);
                }
                break;
                
            case MoveState.Run:
                if (!_hasMovementInput)
                {
                    TransitionToState(MoveState.RunEnd);
                }
                break;
                
            case MoveState.RunEnd:
                if (IsAnimationFinished("Run_End"))
                {
                    Player.ChangeState(PlayerStateType.Idle);
                }
                if (_hasMovementInput)
                {
                    TransitionToState(MoveState.RunStart);
                }
                break;
                
            case MoveState.RunStartEnd:
                if (IsAnimationFinished("Start_End"))
                {
                    Player.ChangeState(PlayerStateType.Idle);
                }
                if (_hasMovementInput)
                {
                    TransitionToState(MoveState.RunStart);
                }
                break;
                
            case MoveState.TurnBack:
                if (!_hasMovementInput)
                {
                    TransitionToState(MoveState.RunEnd);
                }
                else if (IsAnimationFinished("Turn_Back"))
                {
                    TransitionToState(MoveState.Run);
                }
                break;
                
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void TransitionToState(MoveState newState)
    {
        _currentMoveState = newState;
        
        switch (newState)
        {
            case MoveState.RunStart:
                Player.PlayAnimation("Run_Start");
                break;
            case MoveState.Run:
                Player.PlayAnimation("Run");
                break;
            case MoveState.RunEnd:
                Player.PlayAnimation("Run_End");
                break;
            case MoveState.RunStartEnd:
                Player.PlayAnimation("Start_End");
                break;
            case MoveState.TurnBack:
                Player.PlayAnimation("Turn_Back");
                break;
        }
    }

    public override void Exit()
    {
        Player.playerModel.ClearRootMotionAction();
    }
}