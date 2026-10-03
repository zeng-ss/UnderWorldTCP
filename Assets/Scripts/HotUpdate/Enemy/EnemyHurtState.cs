using UnityEngine;

public class EnemyHurtState : Enemy_State
{
    private Vector3 hurtPos;
    private bool isHurtFront;
    public override void Enter()
    {
        if (!enemy.IsServer) return;
        enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        CheckHurtState();
        // 15%概率打断受伤进入攻击，85%概率正常播放受伤动画
        if (Random.Range(0f, 1f) <= 0.15f)
        {
            // 反击攻击
            enemy.isCanPlayHurtAni = false;
            enemy.ChangeState(EnemyStateType.Attack);
        }
        else // 正常播放受伤动画
        {
            enemy.isCanPlayHurtAni = true;
        }
    }

    private void OnRootMotion(Vector3 arg1, Quaternion arg2) { enemy.characterController.Move(arg1); }

    public override void Update()
    {
        if (!enemy.IsServer) return;
        if (enemy.PlayerRef == null) return;
        if (isHurtFront && Vector3.Distance(enemy.transform.position, enemy.PlayerRef.transform.position) <= 1.2f) enemy.FaceToPlayer();
        if (IsAnimationMoreThanTime("HurtFront",0.8f) || IsAnimationMoreThanTime("HurtBack",0.8f)) { enemy.ChangeState(EnemyStateType.Idle); }
    }

    public void SetHurtPos(Vector3 pos) { hurtPos = pos; }

    private void CheckHurtState()
    {
        Vector3 toAttacker = (hurtPos - enemy.transform.position).normalized;
        float dot = Vector3.Dot(enemy.transform.forward, toAttacker);
        isHurtFront = dot > 0;
        // 后受伤 前受伤
        enemy.PlayAnimation(dot > 0 ? "HurtFront" : "HurtBack");
    }

    public override void Exit()
    {
        enemy.enemyModel.ClearRootMotionAction();
    }
}
