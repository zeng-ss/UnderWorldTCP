using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class ExAttackState : PlayerState
{
    private Transform _targetEnemy;
    private bool _isDistanceLocked;

    private enum ExState
    {
        Start,
        Attack,
        PutBack,
        End
    }

    private ExState _curAttackState;

    public override void Enter()
    {
        if (!Player.Core.IsLocalPlayer) return;
        _targetEnemy = FindNearestEnemyByTag();
        // 播放音效
        AppContext.Sound.PlaySound
        (AppContext.Sound.ExSounds[Random.Range(0, AppContext.Sound.ExSounds.Count)],
            Player.transform.position);
        // 进入开始状态
        TransitionAttackState(ExState.Start);
        Player.Core.IsLock = true;
        Player.CameraBinder.VirtualCameraEx.gameObject.SetActive(true);
    }

    public override void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        HandleAttackStateTransitions(h, v);
    }

    private void HandleAttackStateTransitions(float h, float v)
    {
        switch (_curAttackState)
        {
            case ExState.Start:
                _isDistanceLocked = true;
                FaceToEnemy();
                if (IsAnimationMoreThanTime("AttackEx_Start", 0.8f))
                    Player.CameraBinder.VirtualCameraEx.gameObject.SetActive(false);
                if (IsAnimationFinished("AttackEx_Start"))
                {
                    Player.SkillCombo.StartSkillByIndex(0);
                    TransitionAttackState(ExState.Attack);
                }

                break;
            case ExState.Attack:
                if (IsAnimationFinished("AttackEx")) TransitionAttackState(ExState.PutBack);
                break;
            case ExState.PutBack:
                _isDistanceLocked = false;
                Player.SkillCombo.UpdateSkillConfig(ComboSet.Normal);
                if (h != 0 || v != 0)
                {
                    Player.StateMachine.ChangeTo(PlayerStateType.Move);
                    return;
                }

                if (IsAnimationFinished("AttackEx_PutBack")) TransitionAttackState(ExState.End);
                break;
            case ExState.End:
                Player.Core.IsLock = false;
                if (h != 0 || v != 0)
                {
                    Player.StateMachine.ChangeTo(PlayerStateType.Move);
                    return;
                }

                if (IsAnimationFinished("AttackEx_End"))
                {
                    Player.Core.IsLock = false;
                    Player.StateMachine.ChangeTo(PlayerStateType.Idle);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TransitionAttackState(ExState state)
    {
        _curAttackState = state;
        switch (state)
        {
            case ExState.Start: Player.PlayAnimation("AttackEx_Start"); break;
            case ExState.Attack: break;
            case ExState.PutBack: Player.PlayAnimation("AttackEx_PutBack"); break;
            case ExState.End: Player.PlayAnimation("AttackEx_End"); break;
            default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    #region 强制面向敌人 保持与敌人的锁定攻击距离 标签检测最近的敌人

    // 强制面向敌人
    private void FaceToEnemy()
    {
        if (_targetEnemy == null || !_isDistanceLocked) return;
        Vector3 toEnemy = _targetEnemy.position - Player.transform.position;
        toEnemy.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(toEnemy);
        Player.playerModel.transform.rotation = Quaternion.Slerp(
            Player.playerModel.transform.rotation, targetRot, Time.deltaTime * 20f);
        KeepLockDistance();
    }

    // 保持与敌人的锁定攻击距离
    private void KeepLockDistance()
    {
        Vector3 toEnemy = _targetEnemy.position - Player.transform.position;
        toEnemy.y = 0;
        // 计算目标位置：敌人位置 - 朝向角色的单位向量 * 锁定距离
        Vector3 targetPos = _targetEnemy.position - toEnemy.normalized * 2;
        targetPos.y = Player.transform.position.y;
        Player.transform.position = Vector3.Lerp(Player.transform.position, targetPos, Time.deltaTime * 15f);
    }

    // 标签检测最近的敌人
    private Transform FindNearestEnemyByTag()
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("enemy");
        if (allEnemies.Length == 0) return null;

        Transform nearestEnemy = null;
        float minDistance = Mathf.Infinity;
        foreach (GameObject enemyObj in allEnemies)
        {
            float dis = Vector3.Distance(Player.transform.position, enemyObj.transform.position);
            if (dis < minDistance && dis <= 15f)
            {
                minDistance = dis;
                nearestEnemy = enemyObj.transform;
            }
        }

        return nearestEnemy;
    }

    #endregion

    public override void Exit()
    {
        Player.CameraBinder.VirtualCameraEx.gameObject.SetActive(false);
        Player.Core.IsLock = false;
    }
}