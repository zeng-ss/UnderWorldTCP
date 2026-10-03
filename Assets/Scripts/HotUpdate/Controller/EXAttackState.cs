using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class EXAttackState : Player_State
{
    private Transform targetEnemy;
    private bool isDistanceLocked;
    private enum EXState
    {
        Start,
        Attack,
        PutBack,
        End
    }
    private EXState curAttackState;
    
    public override void Enter()
    {
        if (!_player.IsLocalPlayer) return;
        targetEnemy = FindNearestEnemyByTag();
        // 播放音效
        SoundManager.Instance.PlaySound
            (SoundManager.Instance.exSounds[Random.Range(0, SoundManager.Instance.exSounds.Count)], _player.transform.position);
        // 进入开始状态
        TransitionAttackState(EXState.Start);
        _player.isLock = true;
        _player.virtualCameraEx.gameObject.SetActive(true);
    }

    public override void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        HandleAttackStateTransitions(h, v);
    }
    
    private void HandleAttackStateTransitions(float h,  float v)
    {
        switch (curAttackState)
        {
            case EXState.Start:
                isDistanceLocked = true;
                FaceToEnemy();
                if (IsAnimationMoreThanTime("AttackEx_Start",0.8f)) _player.virtualCameraEx.gameObject.SetActive(false);
                if (IsAnimationFinished("AttackEx_Start"))
                {
                    _player.StartSkill(_player.curSkillConfig.skillConfigs[0]);
                    TransitionAttackState(EXState.Attack);
                }
                break;
            case EXState.Attack:
                if (IsAnimationFinished("AttackEx")) TransitionAttackState(EXState.PutBack);
                break;
            case EXState.PutBack:
                isDistanceLocked = false;
                _player.UpdateSkillConfig(0);
                if (h != 0 || v != 0)
                {
                    _player.ChangeState(PlayerStateType.Move);
                    return;
                }
                if (IsAnimationFinished("AttackEx_PutBack")) TransitionAttackState(EXState.End);
                break;
            case EXState.End:
                _player.isLock = false;
                if (h != 0 || v != 0)
                {
                    _player.ChangeState(PlayerStateType.Move);
                    return;
                }
                if (IsAnimationFinished("AttackEx_End"))
                {
                    _player.isLock = false;
                    _player.ChangeState(PlayerStateType.Idle);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TransitionAttackState(EXState state)
    {
        curAttackState = state;
        switch (state)
        {
            case EXState.Start: _player.PlayAnimation("AttackEx_Start"); break;
            case EXState.Attack: break;
            case EXState.PutBack: _player.PlayAnimation("AttackEx_PutBack"); break;
            case EXState.End: _player.PlayAnimation("AttackEx_End"); break;
            default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
    
    #region 强制面向敌人 保持与敌人的锁定攻击距离 标签检测最近的敌人

    // 强制面向敌人
    private void FaceToEnemy()
    {
        if (targetEnemy == null || !isDistanceLocked) return;
        Vector3 toEnemy = targetEnemy.position - _player.transform.position;
        toEnemy.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(toEnemy);
        _player.playerModel.transform.rotation = Quaternion.Slerp(
            _player.playerModel.transform.rotation, targetRot, Time.deltaTime * 20f);
        KeepLockDistance();
    }

    // 保持与敌人的锁定攻击距离
    private void KeepLockDistance()
    {
        Vector3 toEnemy = targetEnemy.position - _player.transform.position;
        toEnemy.y = 0;
        // 计算目标位置：敌人位置 - 朝向角色的单位向量 * 锁定距离
        Vector3 targetPos = targetEnemy.position - toEnemy.normalized * 2;
        targetPos.y = _player.transform.position.y;
        _player.transform.position = Vector3.Lerp(_player.transform.position, targetPos, Time.deltaTime * 15f);
    }
    
    /// <summary>
    /// 标签检测最近的敌人
    /// </summary>
    private Transform FindNearestEnemyByTag()
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("enemy");
        if (allEnemies.Length == 0) return null;

        Transform nearestEnemy = null;
        float minDistance = Mathf.Infinity;
        foreach (GameObject enemyObj in allEnemies)
        {
            float dis = Vector3.Distance(_player.transform.position, enemyObj.transform.position);
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
        _player.virtualCameraEx.gameObject.SetActive(false);
        _player.isLock = false;
    }
    
}
