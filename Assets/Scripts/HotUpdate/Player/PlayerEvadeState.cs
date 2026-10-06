using UnityEngine;

public class PlayerEvadeState : PlayerState
{ 
    public override void Enter()
    {
        if (!Player.IsLocalPlayer) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            Player.ChangeState(PlayerStateType.Idle);
            return;
        }
        Player.playerModel.SetRootMotionAction(OnRootMation);
        Player.PlayAnimation("Evade");
    }
    private void OnRootMation(Vector3 arg1, Quaternion arg2) { Player.CharacterController.Move(arg1); }

    public override void Update()
    {
        // 只允许本地玩家执行逻辑
        if (!Player.IsLocalPlayer) return;
        if (!InputManager.Instance.IsGameplayInputEnabled) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            Player.ChangeState(PlayerStateType.Idle);
            return;
        }
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        // 等待动画播放完成
        if (IsAnimationMoreThanTime("Evade",0.5f))
        {
            Player.ChangeState(PlayerStateType.Idle);
            return;
        }

        if (IsAnimationMoreThanTime("Evade",0.3f))
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                Player.PlayAnimation("Evade");
            }
            else if (h != 0 || v != 0)
            {
                Player.ChangeState(PlayerStateType.Move);
            }
        }
    }
    
    public void SetEvade(bool isShiftDoubleTap)
    {
        Player.playerModel.Animator.SetFloat("EvadeDirection", isShiftDoubleTap ? 1 : 0);
    }

    public override void Exit()
    {
        Player.playerModel.ClearRootMotionAction();
    }
}