using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyAttackState : Enemy_State
{
    private enum AttackState
    {
        Attack01,
        Attack02,
        Attack03,
        Attack04,
        Attack05,
        Attack05Start,
        Attack05Run,
        Attack05Miss,
    }
    private AttackState curAttackState;
    public override void Enter()
    {
        if (!enemy.IsLocalEnemy) return;
        enemy.isCanPlayHurtAni = false;
        enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        enemy.isStartLock = true;
        if (enemy.PlayerRef is null)
        {
            Debug.LogWarning("Enter时没有玩家，退出攻击状态");
            enemy.ChangeState(EnemyStateType.Idle);
            return;
        }
        TransitionAttackState(GetTargetAttackState());
        // 如果不是奔跑攻击状态才开启拼刀提示声音
        if (curAttackState != AttackState.Attack05Start)
        {
            enemy.isStartPinTip.gameObject.SetActive(true);
            SoundManager.Instance.PlaySound(SoundManager.Instance.startPinSound, enemy.transform.position);
        }
    }
    private void OnRootMotion(Vector3 arg1, Quaternion arg2)
    {
        enemy.characterController.Move(arg1);
    }

    public override void Update()
    {
        // 检查玩家是否有效
        if (enemy.PlayerRef is null)
        {
            if (IsAnimationFinished(GetCurAniName())) { enemy.ChangeState(EnemyStateType.Idle); return; }
        }
        if (enemy.currentState != EnemyStateType.Attack) return;
        HandleAttackStateTransitions();
    }

    private AttackState GetTargetAttackState()
    {
        if (enemy.disToPlayer >= 10) { return AttackState.Attack05Start; }
        AttackState[] attacks = 
        {
            AttackState.Attack01,
            AttackState.Attack02,
            AttackState.Attack03,
            AttackState.Attack04,
        };
        return attacks[Random.Range(0, attacks.Length)];
    }

    private string GetCurAniName()
    {
        return curAttackState switch
        {
            AttackState.Attack01 => "Attack01",
            AttackState.Attack02 => "Attack02",
            AttackState.Attack03 => "Attack03",
            AttackState.Attack04 => "Attack04",
            AttackState.Attack05 => "Attack05",
            AttackState.Attack05Start => "Attack05Start",
            _ => null
        };
    }

    private void HandleAttackStateTransitions()
    {
        switch (curAttackState)
        {
            /*case AttackState.Attack01:
                if (IsAnimationFinished("Attack01")) enemy.ChangeState(EnemyStateType.Idle); 
                break;
            case AttackState.Attack02:  
                if (IsAnimationFinished("Attack02")) enemy.ChangeState(EnemyStateType.Idle);
                break;
            case AttackState.Attack03:
                if (IsAnimationFinished("Attack03")) enemy.ChangeState(EnemyStateType.Idle);
                break;
            case AttackState.Attack04:
                if (IsAnimationFinished("Attack04")) enemy.ChangeState(EnemyStateType.Idle);
                break;
            case AttackState.Attack05:
                if (IsAnimationFinished("Attack05")) enemy.ChangeState(EnemyStateType.Idle);
                break;*/
            case AttackState.Attack05Start:
                if (enemy.disToPlayer <= 5) TransitionAttackState(AttackState.Attack05);
                if (IsAnimationFinished("Attack05Start")) TransitionAttackState(AttackState.Attack05Run);
                break;
            case AttackState.Attack05Run:
                if (enemy.disToPlayer <= 5) TransitionAttackState(AttackState.Attack05);
                break;
            /*case AttackState.Attack05Miss:
                break;
            default:
                throw new ArgumentOutOfRangeException();*/
        }
    }

    private void TransitionAttackState(AttackState state)
    {
        curAttackState = state;
        switch (state)
        {
            case AttackState.Attack01: enemy.PlayAnimation("Attack01"); break;
            case AttackState.Attack02: enemy.PlayAnimation("Attack02"); break;
            case AttackState.Attack03: enemy.PlayAnimation("Attack03"); break;
            case AttackState.Attack04: enemy.PlayAnimation("Attack04"); break;
            case AttackState.Attack05: enemy.PlayAnimation("Attack05"); break;
            case AttackState.Attack05Start: enemy.PlayAnimation("Attack05Start"); break;
            case AttackState.Attack05Run: enemy.PlayAnimation("Attack05Run"); break;
            case AttackState.Attack05Miss: enemy.PlayAnimation("Attack05Miss"); break;
            default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public override void Exit()
    {
        enemy.isStartPinTip.gameObject.SetActive(false);
        enemy.isStartPin = false;
        enemy.isPinFinished = false;
        enemy.enemyModel.ClearRootMotionAction();
    }
}
