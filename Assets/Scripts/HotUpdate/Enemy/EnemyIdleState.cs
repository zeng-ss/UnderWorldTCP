using UnityEngine;

public class EnemyIdleState : EnemyState
{
    private float _randomValue;
    private float _timer;

    public override void Enter()
    {
        Enemy.isCanPlayHurtAni = true;
        Enemy.PlayAnimation("Idle");
        _randomValue = Random.Range(3f, 8f);
    }

    public override void Update()
    {
        if (Enemy.IsLocalEnemy) return;
        if (Enemy.PlayerRef == null)
        {
            _timer = 0;
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _randomValue)
        {
            Enemy.ChangeState(EnemyStateType.Attack);
            _randomValue = Random.Range(3f, 8f);
            _timer = 0f;
        }
    }

    public override void Exit()
    {
        _timer = 0f;
    }
}