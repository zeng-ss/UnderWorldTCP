using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttackState : Player_State
{
    #region 数据

    private bool isPlayingEndAni;          // 是否正在播放结束收招动画
    private int playingAttackIndex;        // 记录当前正在播放动画的攻击下标
    private SkillConfig cacheSkillConfig;  // 缓存切换连招前的旧配置表
    
    // 攻击目标与距离控制参数（可在Inspector面板调整）
    [Header("攻击目标配置")]
    private float attackDetectRange = 15f;   // 敌人检测范围
    private float normalAttackRange = 1.5f;  // 普通攻击有效距离
    private float rushAttackRange = 8f;      // 冲刺杀检测范围
    private float rushMoveSpeed = 18f;       // 冲刺位移速度
    private float lockAttackDistance = 2f;   // 攻击锁定距离（角色-敌人）

    private Transform targetEnemy;  // 锁定的最近敌人
    private bool isDistanceLocked;  // 是否已到达攻击范围并锁定距离
    private bool isRushAttack;      // 是否是第二套连招的冲刺杀

    #endregion
    
    public override void Enter()
    {
        if (!_player.IsLocalPlayer) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }
        _player.playerModel.SetRootMotionAction(OnRootMotion);
        //_player.CurAttackIndex = 0;
        playingAttackIndex = _player.CurAttackIndex; // 初始化播放下标
        cacheSkillConfig = _player.CurSkillConfig;
        // 初始化攻击目标参数
        targetEnemy = FindNearestEnemyByTag();
        isDistanceLocked = false;
        isRushAttack = false;

        // 检测是否是第二套连招的冲刺杀（第一段）
        CheckRushAttack();
        Attack();
    }
    public override void Update()
    {
        if (!_player.IsLocalPlayer) return;
        // 任何独占型面板打开时都锁住玩法输入，状态内部不再需要各自判断
        if (!InputManager.Instance.IsGameplayInputEnabled) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }
        // 顿帧逻辑优先处理（最高优先级）
        if (isInHitStop)
        {
            HandleHitStop();
            return; // 顿帧期间完全屏蔽其他逻辑
        }
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool hasInput = h != 0 || v != 0;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            _player.CurAttackIndex = -1; // 重置为-1，自增后正好是0
            isRushAttack = false;
            isDistanceLocked = false;
        }
        // 如果正在播放 EndAni ，只检测是否播完，不再执行其他逻辑
        if (isPlayingEndAni)
        {
            // 收招逻辑用缓存的旧配置表 + 播放下标，避免访问新配置表越界
            if (cacheSkillConfig == null || playingAttackIndex >= cacheSkillConfig.skillConfigs.Count)
            {
                _player.ChangeState(PlayerStateType.Idle);
                return;
            }
            string endName = GetCurrentEndAni(cacheSkillConfig.skillConfigs[playingAttackIndex].attackAnimationName);
            if (endName == "-1" || IsAnimationFinished(endName) || hasInput)
            {
                _player.ChangeState(PlayerStateType.Idle);
            }
            return;
        }

        // 判断当前攻击动画是否播放完毕 攻击动画判断用缓存的旧配置表 + 播放下标
        if (cacheSkillConfig == null || playingAttackIndex >= cacheSkillConfig.skillConfigs.Count)
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }
        string currentAttackAnim = cacheSkillConfig.skillConfigs[playingAttackIndex].attackAnimationName;
        if (IsAnimationFinished(currentAttackAnim))
        {
            // 攻击动画结束  播放收招动画
            _player.PlayAnimation(GetCurrentEndAni(currentAttackAnim));
            isPlayingEndAni = true; // 标记进入 EndAni 等待逻辑
            return;
        }

        // 特定没有结束动画的情况提前可以移动打断
        if (hasInput && IsAnimationMoreThanTime(currentAttackAnim,0.5f) && currentAttackAnim is "AttackBranch01" or "AttackB01Per")
        {
            _player.ChangeState(PlayerStateType.Idle);
            return;
        }

        // 重击逻辑
        // 第一套连招重击逻辑
        if (_player.CanSwitchSkill 
            && Input.GetKeyDown(KeyCode.Mouse2)
            && currentAttackAnim == "Attack03"
            && cacheSkillConfig == _player.SkillConfigList[0])
        {
            // 直接切到最后一个重击
            _player.CurAttackIndex = cacheSkillConfig.skillConfigs.Count - 1;
            playingAttackIndex = _player.CurAttackIndex;
            isDistanceLocked = false; 
            CheckRushAttack(); 
            Attack();
            return;
        }
        
        // 全重击逻辑
        if (_player.CanSwitchSkill && Input.GetKeyDown(KeyCode.Mouse1) && currentAttackAnim != "Attack03")
        {
            // 切换到 skillConfigList 中的重击配置表
            _player.CurAttackIndex ++; // 重击配置表第一段
            cacheSkillConfig = _player.SkillConfigList[2];
            playingAttackIndex = _player.CurAttackIndex;
            isDistanceLocked = false; 
            Attack();
            return;
        }
        
        // 左键正常连招逻辑（跳过重击回到第一个）
        if (_player.CanSwitchSkill && Input.GetKeyDown(KeyCode.Mouse0) && _player.CurSkillConfig != _player.SkillConfigList[2])
        {
            // 判断是否到了倒数第二个攻击（重击的前一个）
            if (currentAttackAnim == "Attack04")
            {
                // 跳过重击，直接回到第一个攻击
                _player.CurAttackIndex = 0;
            }
            else
            {
                // 没到倒数第二个，正常自增
                _player.CurAttackIndex++;
            }
            playingAttackIndex = _player.CurAttackIndex;
            cacheSkillConfig = _player.CurSkillConfig;
            isDistanceLocked = false; 
            CheckRushAttack(); 
            Attack();
            return;
        }
        
        // 距离锁定后：强制保持与敌人的距离 + 面向，忽略移动输入
        if (isDistanceLocked && targetEnemy)
        {
            KeepLockDistance();
            FaceToEnemy();
            return;
        }
        
        // 旋转逻辑（仅无敌人/未锁定时生效，有敌人时强制面向）
        HandRotate(h, v, hasInput);
        if (_player.CanSwitchSkill && Input.GetKeyDown(KeyCode.LeftShift)) _player.ChangeState(PlayerStateType.Evade);
    }

    #region 顿帧

    // 原有变量保留，新增以下变量
    public bool isInHitStop; // 顿帧标记
    private float hitStopDuration; // 顿帧持续时间
    private float hitStopEndTime; // 顿帧结束时间
    private float originalTimeScale; // 原始动画速度（用于恢复）
    // 顿帧强度配置（可调整）
    [Header("顿帧配置")]
    private float hitStopTimeScale; // 顿帧时的时间缩放（0=完全暂停，0.1=轻微慢放）
    private float hitStopDurationDefault = 0.08f; // 顿帧持续时间
    private float animSpeedDuringHitStop; // 顿帧时动画速度（0=冻结帧）
    
    public void EnterHitStop(float duration = 0f, float timeScale = 0f)
    {
        // 避免重复触发顿帧
        if (isInHitStop || !cacheSkillConfig.skillConfigs[playingAttackIndex].vfxDataList[_player.CurVFXIndex].isInHitStop)
            return;
        // 1. 记录原始状态（用于恢复）
        originalTimeScale = Time.timeScale;
        hitStopDuration = duration <= 0 ? hitStopDurationDefault : duration;
        float targetTimeScale = timeScale <= 0 ? hitStopTimeScale : timeScale;
        // 2. 启动顿帧
        isInHitStop = true;
        hitStopEndTime = Time.unscaledTime + hitStopDuration; // 用unscaledTime避免时间缩放影响
        // 3. 真顿帧：暂停全局时间（仅战斗相关，UI不受影响）
        Time.timeScale = targetTimeScale;
        // 4. 冻结动画：直接停在当前帧，而非慢放
        _player.playerModel.Animator.speed = animSpeedDuringHitStop;
        _player.playerModel.Animator.Update(0); // 强制更新动画，冻结当前帧
    }
    
    // 顿帧处理专用方法
    private void HandleHitStop()
    {
        // 用unscaledTime判断顿帧是否结束（不受Time.timeScale影响）
        if (Time.unscaledTime >= hitStopEndTime)
        {
            // 1. 恢复时间缩放
            Time.timeScale = originalTimeScale;
            // 2. 恢复动画速度（立即恢复，保证跟手）
            _player.playerModel.Animator.speed = 1;
            // 4. 重置顿帧标记
            isInHitStop = false;
            return;
        }
        // 顿帧期间：强制冻结动画+屏蔽所有输入
        _player.playerModel.Animator.Update(0);
    }

    #endregion
    
    #region 攻击和旋转

    private void Attack()
    {
        isPlayingEndAni = false;
        if (_player.CurAttackIndex == -1) _player.CurAttackIndex = 0;
        _player.StartSkill(_player.CurSkillConfig.skillConfigs[_player.CurAttackIndex]);
    }

    private void HandRotate(float h, float v, bool hasInput)
    {
        if (_player.CurVFXIndex >= cacheSkillConfig.skillConfigs[playingAttackIndex].vfxDataList.Count) return;
        if (!isDistanceLocked && !targetEnemy
                              && cacheSkillConfig.skillConfigs[playingAttackIndex].vfxDataList[_player.CurVFXIndex].canRotate)
        {
            if (!hasInput) return;
            Vector3 input = new Vector3(h, 0, v);
            float y = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;
            _player.playerModel.transform.rotation = Quaternion.Slerp
                (_player.playerModel.transform.rotation, Quaternion.LookRotation(moveDir), _player.rotationSpeed * Time.deltaTime);
        }
    }

    #endregion
    
    #region 根运动

    // 原生根运动回调，仅做距离判断，面向敌人
    private void OnRootMotion(Vector3 rootMotion, Quaternion rootRot)
    {
        // 已锁定距离/冲刺杀：停止根运动位移，仅保留旋转
        if (isDistanceLocked || isRushAttack)
        {
            _player.playerModel.transform.rotation *= rootRot;
            return;
        }
        if (targetEnemy == null)
        {
            rootMotion.y = 0; 
            _player.CharacterController.Move(rootMotion);
            // 仅保留XZ轴旋转，清空Y轴旋转（防止角色倾斜/震动）
            Quaternion rot = rootRot;
            rot.x = 0;
            rot.z = 0;
            _player.playerModel.transform.rotation *= rot;
            return;
        }
        // 有敌人：计算与敌人距离，判断是否锁定
        Vector3 toEnemy = targetEnemy.position - _player.transform.position;
        toEnemy.y = 0; // 忽略Y轴，保持水平检测
        float distanceToEnemy = toEnemy.magnitude;

        // 到达攻击范围：锁定距离+触发击退，停止根运动
        if (distanceToEnemy <= normalAttackRange)
        {
            isDistanceLocked = true;
            return;
        }

        // 未到范围：执行原生根运动  强制面向敌人
        rootMotion.y = 0;
        _player.CharacterController.Move(rootMotion);
        FaceToEnemy();
    }
    private void OnRootMotionNormal(Vector3 rootMotion, Quaternion rootRot)
    {
        _player.CharacterController.Move(rootMotion);
    }

    #endregion
    
    #region 冲刺杀

    private Coroutine rushToEnemyCoroutine;
    // 检测是否是第二套连招的冲刺杀（第一段）
    private void CheckRushAttack()
    {
        // 判断条件：第二套连招（skillConfigList[1]）+ 第一段攻击 + 动画是冲刺杀
        if (CheckRushOrPowerAttack())
        {
            isRushAttack = true;
            // 敌人在冲刺范围内：启动冲刺协程，跳过根运动
            if (targetEnemy != null && Vector3.Distance(_player.transform.position, targetEnemy.position) <= rushAttackRange)
            {
                rushToEnemyCoroutine = MonoManager.Instance.StartCoroutine(RushToEnemyCoroutine());
            }
            else { _player.playerModel.SetRootMotionAction(OnRootMotionNormal); }
        }
    }

    // 冲刺杀协程：位移到敌人锁定距离，触发击退
    private IEnumerator RushToEnemyCoroutine()
    {
        _player.playerModel.ClearRootMotionAction(); // 关闭根运动，避免冲突
        while (targetEnemy != null && Vector3.Distance(_player.transform.position, targetEnemy.position) > lockAttackDistance)
        {
            Vector3 dir = (targetEnemy.position - _player.transform.position).normalized;
            dir.y = 0;
            _player.CharacterController.Move(dir * (rushMoveSpeed * Time.deltaTime));
            FaceToEnemy(); // 冲刺中始终面向敌人
            yield return null;
        }
        // 冲刺到位：锁定距离 + 触发击退
        if (targetEnemy != null) { isDistanceLocked = true; }
    }

    private bool CheckRushOrPowerAttack()
    {
        return playingAttackIndex == 0
               && cacheSkillConfig == _player.SkillConfigList[1] && cacheSkillConfig.skillConfigs[0].attackAnimationName == "AttackRush";
    }

    #endregion
    
    #region 强制面向敌人,保持与敌人的锁定攻击距离,标签检测最近的敌人

    // 强制面向敌人
    private void FaceToEnemy()
    {
        if (targetEnemy == null) return;
        Vector3 toEnemy = targetEnemy.position - _player.transform.position;
        toEnemy.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(toEnemy);
        _player.playerModel.transform.rotation = Quaternion.Slerp(
            _player.playerModel.transform.rotation, targetRot, Time.deltaTime * 20f);
    }

    // 保持与敌人的锁定攻击距离
    private void KeepLockDistance()
    {
        Vector3 toEnemy = targetEnemy.position - _player.transform.position;
        toEnemy.y = 0;
        // 计算目标位置：敌人位置 - 朝向角色的单位向量 * 锁定距离
        Vector3 targetPos = targetEnemy.position - toEnemy.normalized * lockAttackDistance;
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
            if (dis < minDistance && dis <= attackDetectRange)
            {
                minDistance = dis;
                nearestEnemy = enemyObj.transform;
            }
        }
        return nearestEnemy;
    }

    #endregion
    
    #region 收招动画映射

    // 收招动画映射
    private string GetCurrentEndAni(string attackAniName)
    {
        return attackAniName switch
        {
            "Attack01" => "Attack01End",
            "Attack02" => "Attack02End",
            "Attack03" => "Attack03End",
            "Attack04" => "Attack04End",
            "Attack04Perfect" => "Attack04PerfectEnd",
            "AttackRush" => "AttackRushEnd",
            "AttackB04Per" => "AttackB04PerEnd",
            "AttackBranch04" => "AttackBranch04End",
            _ => "-1"
        };
    }

    #endregion
    
    public override void Exit()
    {
        isPlayingEndAni = false;
        _player.CurAttackIndex = 0;
        if (rushToEnemyCoroutine != null)
        {
            MonoManager.Instance.StopCoroutine(rushToEnemyCoroutine);
            rushToEnemyCoroutine = null;
        }
        _player.playerModel.ClearRootMotionAction();
    }
    
}