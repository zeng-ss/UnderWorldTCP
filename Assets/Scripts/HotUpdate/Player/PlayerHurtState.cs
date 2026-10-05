using UnityEngine;

public class PlayerHurtState : Player_State
{
    public override void Enter()
    {
        if (!_player.IsLocalPlayer) return;
        _player.playerModel.SetRootMotionAction(OnRootMotion);
        _player.PlayAnimation("Hurt");
    }

    private void OnRootMotion(Vector3 arg1, Quaternion arg2) { _player.CharacterController.Move(arg1); }
    public override void Update()
    {
        if (!_player.IsLocalPlayer) return;
        if (IsAnimationMoreThanTime("Hurt", 0.5f)) { _player.ChangeState(PlayerStateType.Idle); }
    }

    public override void Exit()
    {
        
    }
}
