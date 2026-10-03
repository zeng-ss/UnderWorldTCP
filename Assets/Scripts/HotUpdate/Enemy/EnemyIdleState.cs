using UnityEngine;

public class EnemyIdleState : Enemy_State
{
    private float randomValue;
    private float timer;
    public override void Enter()
    {
        enemy.isCanPlayHurtAni = true;
        enemy.PlayAnimation("Idle");
        randomValue = Random.Range(3f, 8f);
    }

    public override void Update()
    {
        if (enemy.IsLocalEnemy) return;
        if (enemy.PlayerRef == null) { timer = 0; return; }
        timer += Time.deltaTime;
        if (timer >= randomValue)
        {
            enemy.ChangeState(EnemyStateType.Attack);
            randomValue = Random.Range(3f, 8f);
            timer = 0f;
        }
    }

    public override void Exit() { timer = 0f; }
    
}
