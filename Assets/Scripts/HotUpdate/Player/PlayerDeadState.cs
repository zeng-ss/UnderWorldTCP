namespace HotUpdate.Player
{
    public class PlayerDeadState : PlayerState
    {
        public override void Enter()
        {
            if (!Player.Core.IsLocalPlayer) return;
            Player.PlayAnimation("Dead");
        }
    }
}
