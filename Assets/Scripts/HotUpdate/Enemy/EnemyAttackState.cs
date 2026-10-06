using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyAttackState : EnemyState
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

    private AttackState _curAttackState;

    public override void Enter()
    {
        if (!Enemy.IsLocalEnemy) return;
        Enemy.isCanPlayHurtAni = false;
        Enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        Enemy.isStartLock = true;
        if (Enemy.PlayerRef is null)
        {
            Debug.LogWarning("Enter时没有玩家，退出攻击状态");
            Enemy.ChangeState(EnemyStateType.Idle);
            return;
        }

        TransitionAttackState(GetTargetAttackState());
        // 如果不是奔跑攻击状态才开启拼刀提示声音
        if (_curAttackState != AttackState.Attack05Start)
        {
            Enemy.isStartPinTip.gameObject.SetActive(true);
            AppContext.Sound.PlaySound(AppContext.Sound.StartPinSound, Enemy.transform.position);
        }
    }

    private void OnRootMotion(Vector3 arg1, Quaternion arg2)
    {
        Enemy.characterController.Move(arg1);
    }

    public override void Update()
    {
        // 检查玩家是否有效
        if (Enemy.PlayerRef is null)
        {
            if (IsAnimationFinished(GetCurAniName()))
            {
                Enemy.ChangeState(EnemyStateType.Idle);
                return;
            }
        }

        if (Enemy.currentState != EnemyStateType.Attack) return;
        HandleAttackStateTransitions();
    }

    private AttackState GetTargetAttackState()
    {
        if (Enemy.disToPlayer >= 10)
        {
            return AttackState.Attack05Start;
        }

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
        return _curAttackState switch
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
        switch (_curAttackState)
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
                if (Enemy.disToPlayer <= 5) TransitionAttackState(AttackState.Attack05);
                if (IsAnimationFinished("Attack05Start")) TransitionAttackState(AttackState.Attack05Run);
                break;
            case AttackState.Attack05Run:
                if (Enemy.disToPlayer <= 5) TransitionAttackState(AttackState.Attack05);
                break;
            /*case AttackState.Attack05Miss:
                break;
            default:
                throw new ArgumentOutOfRangeException();*/
        }
    }

    private void TransitionAttackState(AttackState state)
    {
        _curAttackState = state;
        switch (state)
        {
            case AttackState.Attack01: Enemy.PlayAnimation("Attack01"); break;
            case AttackState.Attack02: Enemy.PlayAnimation("Attack02"); break;
            case AttackState.Attack03: Enemy.PlayAnimation("Attack03"); break;
            case AttackState.Attack04: Enemy.PlayAnimation("Attack04"); break;
            case AttackState.Attack05: Enemy.PlayAnimation("Attack05"); break;
            case AttackState.Attack05Start: Enemy.PlayAnimation("Attack05Start"); break;
            case AttackState.Attack05Run: Enemy.PlayAnimation("Attack05Run"); break;
            case AttackState.Attack05Miss: Enemy.PlayAnimation("Attack05Miss"); break;
            default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public override void Exit()
    {
        Enemy.isStartPinTip.gameObject.SetActive(false);
        Enemy.isStartPin = false;
        Enemy.isPinFinished = false;
        Enemy.enemyModel.ClearRootMotionAction();
    }
}