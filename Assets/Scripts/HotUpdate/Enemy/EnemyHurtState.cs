using UnityEngine;

public class EnemyHurtState : EnemyState
{
    private Vector3 _hurtPos;
    private bool _isHurtFront;
    public override void Enter()
    {
        if (!Enemy.IsServer) return;
        Enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        CheckHurtState();
        // 15%概率打断受伤进入攻击，85%概率正常播放受伤动画
        if (Random.Range(0f, 1f) <= 0.15f)
        {
            // 反击攻击
            Enemy.isCanPlayHurtAni = false;
            Enemy.ChangeState(EnemyStateType.Attack);
        }
        else // 正常播放受伤动画
        {
            Enemy.isCanPlayHurtAni = true;
        }
    }

    private void OnRootMotion(Vector3 arg1, Quaternion arg2) { Enemy.characterController.Move(arg1); }

    public override void Update()
    {
        if (!Enemy.IsServer) return;
        if (Enemy.PlayerRef == null) return;
        if (_isHurtFront && Vector3.Distance(Enemy.transform.position, Enemy.PlayerRef.transform.position) <= 1.2f) Enemy.FaceToPlayer();
        if (IsAnimationMoreThanTime("HurtFront",0.8f) || IsAnimationMoreThanTime("HurtBack",0.8f)) { Enemy.ChangeState(EnemyStateType.Idle); }
    }

    public void SetHurtPos(Vector3 pos) { _hurtPos = pos; }

    private void CheckHurtState()
    {
        Vector3 toAttacker = (_hurtPos - Enemy.transform.position).normalized;
        float dot = Vector3.Dot(Enemy.transform.forward, toAttacker);
        _isHurtFront = dot > 0;
        // 后受伤 前受伤
        Enemy.PlayAnimation(dot > 0 ? "HurtFront" : "HurtBack");
    }

    public override void Exit()
    {
        Enemy.enemyModel.ClearRootMotionAction();
    }
}
