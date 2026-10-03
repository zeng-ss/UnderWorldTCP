using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMoveState : Player_State
{
    private enum MoveState
    {
        RunStart,   // 起步
        Run,        // 奔跑中
        RunEnd,     // 急停
        RunStartEnd,// 起步立即停止
        TurnBack    // 转身
    }
    
    private MoveState currentMoveState;
    private bool hasMovementInput;
    private float inputMagnitude;
    private Vector3 _lastMoveDirection;
    
    public override void Enter()
    {
        if (!_player.IsLocalPlayer) return;
        _lastMoveDirection = Vector3.zero;
        CheckInput();
        if (hasMovementInput)
        {
            TransitionToState(MoveState.RunStart);
        }
        _player.playerModel.SetRootMotionAction(OnRootMation);
    }

    private void OnRootMation(Vector3 arg1, Quaternion arg2) { _player.characterController.Move(arg1); }

    public override void Update()
    {
        if (!_player.IsLocalPlayer) return;
        if (GameManager.Instance.IsPointerOverSpecificUILayer(LayerMask.GetMask("LockInput")))
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }
        CheckInput();
        HandleMovementTransitions();
        if (Input.GetKeyDown(KeyCode.Mouse0) || 
            (Input.GetKeyDown(KeyCode.Mouse1) && _player.curSkillConfig == _player.skillConfigList[2]))
        {
            _player.ChangeState(PlayerStateType.Attack);
        }
    }
    
    private void CheckInput()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        
        inputMagnitude = new Vector2(horizontal, vertical).magnitude;
        hasMovementInput = inputMagnitude > 0.1f;
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _player.ChangeState(PlayerStateType.Evade);
            return;
        }
        // 计算当前移动方向并处理旋转（所有状态都旋转）
        _player.currentMoveDir = HandleRotation(horizontal, vertical);
        
        // 转身检测 - 只在奔跑状态进行，起步状态不检测转身
        if (currentMoveState == MoveState.Run && 
            hasMovementInput && 
            _lastMoveDirection.magnitude > 0.1f)
        {
            float angle = Vector3.Angle(_lastMoveDirection, _player.currentMoveDir);
            if (angle > 120f)
            {
                TransitionToState(MoveState.TurnBack);
            }
        }
        
        // 更新上一帧的移动方向
        if (_player.currentMoveDir.magnitude > 0.1f)
        {
            _lastMoveDirection = new Vector3(_player.currentMoveDir.x, 0, _player.currentMoveDir.z).normalized;
        }
    }
    
    // 处理旋转 - 所有状态都使用平滑旋转
    private Vector3 HandleRotation(float h, float v) 
    {
        if (!_player.cameraTransform) _player.cameraTransform = Camera.main?.transform;
        Transform camTransform = _player.cameraTransform?.transform;
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
            if (currentMoveState == MoveState.TurnBack)
            {
                _player.playerModel.transform.rotation = targetRotation;
            }
            else
            {
                _player.playerModel.transform.rotation = Quaternion.Slerp(
                    _player.playerModel.transform.rotation, 
                    targetRotation,
                    _player.rotationSpeed * Time.deltaTime
                );
            }
        }
        
        return moveDir;
    }
    
    private void HandleMovementTransitions()
    {
        switch (currentMoveState)
        {
            case MoveState.RunStart:
                if (!hasMovementInput)
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
                if (!hasMovementInput)
                {
                    TransitionToState(MoveState.RunEnd);
                }
                break;
                
            case MoveState.RunEnd:
                if (IsAnimationFinished("Run_End"))
                {
                    _player.ChangeState(PlayerStateType.Idle);
                }
                if (hasMovementInput)
                {
                    TransitionToState(MoveState.RunStart);
                }
                break;
                
            case MoveState.RunStartEnd:
                if (IsAnimationFinished("Start_End"))
                {
                    _player.ChangeState(PlayerStateType.Idle);
                }
                if (hasMovementInput)
                {
                    TransitionToState(MoveState.RunStart);
                }
                break;
                
            case MoveState.TurnBack:
                if (!hasMovementInput)
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
        currentMoveState = newState;
        
        switch (newState)
        {
            case MoveState.RunStart:
                _player.PlayAnimation("Run_Start");
                break;
            case MoveState.Run:
                _player.PlayAnimation("Run");
                break;
            case MoveState.RunEnd:
                _player.PlayAnimation("Run_End");
                break;
            case MoveState.RunStartEnd:
                _player.PlayAnimation("Start_End");
                break;
            case MoveState.TurnBack:
                _player.PlayAnimation("Turn_Back");
                break;
        }
    }

    public override void Exit()
    {
        _player.playerModel.ClearRootMotionAction();
    }
}