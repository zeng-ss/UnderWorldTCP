public class PlayerDeadState : Player_State
{
    public override void Enter()
    {
        if (!_player.IsLocalPlayer) return;
        _player.PlayAnimation("Dead");
    }
}
