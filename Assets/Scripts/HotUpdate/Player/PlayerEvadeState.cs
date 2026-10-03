using UnityEngine;

public class PlayerEvadeState : Player_State
{ 
    public override void Enter()
    {
        if (!_player.IsLocalPlayer) return;
        if (GameManager.Instance.IsPointerOverSpecificUILayer(LayerMask.GetMask("LockInput")))
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }
        _player.playerModel.SetRootMotionAction(OnRootMation);
        _player.PlayAnimation("Evade");
    }
    private void OnRootMation(Vector3 arg1, Quaternion arg2) { _player.characterController.Move(arg1); }

    public override void Update()
    {
        // 只允许本地玩家执行逻辑
        if (!_player.IsLocalPlayer) return;
        if (GameManager.Instance.IsPointerOverSpecificUILayer(LayerMask.GetMask("LockInput")))
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        // 等待动画播放完成
        if (IsAnimationMoreThanTime("Evade",0.5f))
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }

        if (IsAnimationMoreThanTime("Evade",0.3f))
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                _player.PlayAnimation("Evade");
            }
            else if (h != 0 || v != 0)
            {
                _player.ChangeState(PlayerStateType.Move);
            }
        }
    }
    
    public void SetEvade(bool isShiftDoubleTap)
    {
        _player.playerModel.Animator.SetFloat("EvadeDirection", isShiftDoubleTap ? 1 : 0);
    }

    public override void Exit()
    {
        _player.playerModel.ClearRootMotionAction();
    }
}