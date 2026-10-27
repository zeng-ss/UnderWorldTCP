namespace HotUpdate.Enemy
{
    /// <summary>
    /// 死亡：播 Dead 后停在最后一帧（销毁交给服务端 / 上层计时）。
    /// </summary>
    public class EnemyDeadState : Enemy_State
    {
        public override void Enter()
        {
            if (!enemy.IsHost) return;

            enemy.HurtAnimAllowed = false;
            enemy.PlayAnimation("Dead");
        }
    }
}
