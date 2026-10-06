
public class EnemyDeadState : EnemyState
{
    public override void Enter()
    {
        if (!Enemy.IsServer) return;
        Enemy.PlayAnimation("Dead");
        Enemy.characterController.enabled = false;
        Enemy.tag = "Untagged";
    }
}
