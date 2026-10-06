public class PlayerDeadState : PlayerState
{
    public override void Enter()
    {
        if (!Player.IsLocalPlayer) return;
        Player.PlayAnimation("Dead");
    }
}