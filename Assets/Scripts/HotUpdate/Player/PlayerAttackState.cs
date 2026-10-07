using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    #region 攻击生命周期阶段

    private enum AttackPhase
    {
        Active, // 攻击进行中：播攻击动画、消费连招输入接段、距离锁定、旋转
        Recovery, // 收招：播收招动画，播完 / 空配置 / 有移动输入则回 Idle
    }

    private AttackPhase _phase;

    #endregion

    #region 数据

    // 攻击目标与距离控制参数
    private const float AttackDetectRange = 15f; // 敌人检测范围
    private const float NormalAttackRange = 1.5f; // 普通攻击有效距离
    private const float RushAttackRange = 8f; // 冲刺杀检测范围
    private const float RushMoveSpeed = 18f; // 冲刺位移速度
    private const float LockAttackDistance = 2f; // 攻击锁定距离（角色-敌人）

    private Transform _targetEnemy; // 锁定的最近敌人
    private bool _isDistanceLocked; // 是否已到达攻击范围并锁定距离
    private bool _isRushAttack; // 当前段是否是配置里标的冲刺杀

    // 当前正在播放的段数据：从 PlayerSkillCombo 的「播放快照」读取（唯一真相源），
    // 不再在 PlayerAttackState 里自存 _cacheSkillConfig / _playingAttackIndex（双份状态已收敛）。
    private AttackData CachedAttackData => Player.SkillCombo.PlayingAttackData;

    #endregion

    public override void Enter()
    {
        if (!Player.Core.IsLocalPlayer) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            Player.StateMachine.ChangeTo(PlayerStateType.Idle);
            return;
        }

        Player.playerModel.SetRootMotionAction(OnRootMotion);
        _phase = AttackPhase.Active; // 进入攻击状态默认从「攻击进行」阶段开始
        // 初始化攻击目标参数
        _targetEnemy = FindNearestEnemyByTag();
        _isDistanceLocked = false;
        _isRushAttack = false;

        // 配置里标了冲刺杀的段：直接位移到敌人身边
        CheckRushAttack();
        Attack();
    }

    public override void Update()
    {
        if (!Player.Core.IsLocalPlayer) return;
        // 任何独占型面板打开时都锁住玩法输入，状态内部不再需要各自判断
        if (!InputManager.Instance.IsGameplayInputEnabled) return;
        if (InputManager.IsPointerOverBlockingUI())
        {
            Player.StateMachine.ChangeTo(PlayerStateType.Idle);
            return;
        }

        // 顿帧逻辑优先处理（最高优先级，独立守卫，不属于 phase）
        if (IsInHitStop)
        {
            HandleHitStop();
            return; // 顿帧期间完全屏蔽其他逻辑
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool hasInput = h != 0 || v != 0;

        // 当前段数据越界 → 直接回 Idle
        AttackData currentData = CachedAttackData;
        if (currentData == null)
        {
            Player.StateMachine.ChangeTo(PlayerStateType.Idle);
            return;
        }

        // 轻量版 phase 状态机：按当前生命周期阶段分派，各阶段职责单一
        switch (_phase)
        {
            case AttackPhase.Recovery:
                UpdateRecovery(currentData, hasInput);
                break;

            case AttackPhase.Active:
                UpdateActive(currentData, h, v, hasInput);
                break;
        }
    }

    // 收招阶段
    private void UpdateRecovery(AttackData endData, bool hasInput)
    {
        string endName = endData.endAnimationName;
        if (string.IsNullOrEmpty(endName) || IsAnimationFinished(endName) || hasInput)
        {
            Player.StateMachine.ChangeTo(PlayerStateType.Idle);
        }
    }

    // 攻击进行阶段
    private void UpdateActive(AttackData currentData, float h, float v, bool hasInput)
    {
        string currentAttackAnim = currentData.attackAnimationName;

        // 攻击动画播完 → 有收招动画转 Recovery，没配直接回 Idle
        if (IsAnimationFinished(currentAttackAnim))
        {
            if (string.IsNullOrEmpty(currentData.endAnimationName))
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Idle);
                return;
            }

            Player.PlayAnimation(currentData.endAnimationName); // 播放收招动画
            TransitionTo(AttackPhase.Recovery);
            return;
        }

        // 配置标了「可被移动打断」的段：播到可打断时间后，有移动输入就直接收招
        if (hasInput && currentData.canBeInterruptedByMove && IsAnimationMoreThanTime(currentAttackAnim, currentData.interruptibleTime))
        {
            Player.StateMachine.ChangeTo(PlayerStateType.Idle);
            return;
        }

        // 连招决策：消费输入缓冲
        if (Player.SkillCombo.CanSwitchSkill && Player.SkillCombo.TryConsumeAttackInput(out AttackInput attackInput))
        {
            if (HandleComboInput(attackInput, currentData)) return;
        }

        // 距离锁定后：强制保持与敌人的距离 + 面向，忽略移动输入
        if (_isDistanceLocked && _targetEnemy)
        {
            KeepLockDistance();
            FaceToEnemy();
            return;
        }

        // 旋转逻辑
        HandRotate(h, v, hasInput);
    }

    // 连招决策：消费一条缓冲输入并执行对应动作。返回 true 表示已处理（本帧到此为止），
    // false 表示该输入不满足任何分支（如重击表为空），继续走后续的距离锁定 / 旋转逻辑。
    private bool HandleComboInput(AttackInput attackInput, AttackData currentData)
    {
        switch (attackInput)
        {
            // 中键重击：配置标了 isHeavyEntry 的段，按中键直接跳到最后一段
            case AttackInput.HeavyEntry when currentData.isHeavyEntry:
                Player.SkillCombo.CurAttackIndex = Player.SkillCombo.PlayingConfig.Count - 1;
                _isDistanceLocked = false;
                CheckRushAttack();
                Attack();
                return true;

            // 右键重击：已切到重击表则按 nextAttackIndex 接段（自增到下一段，触发第二段），
            // 尚未切到重击表则先切表、从第一段起手
            case AttackInput.Heavy:
            {
                SkillConfig heavyConfig = Player.SkillCombo.GetSkillConfig(ComboSet.Heavy);
                if (heavyConfig == null || currentData.isHeavyEntry) return false;

                if (Player.SkillCombo.IsCurrent(ComboSet.Heavy))
                {
                    // 已在重击连招中：按 nextAttackIndex 衔接（-1 = 自增到下一段，即第二段）
                    Player.SkillCombo.CurAttackIndex = currentData.nextAttackIndex >= 0
                        ? currentData.nextAttackIndex
                        : Player.SkillCombo.CurAttackIndex + 1;
                }
                else
                {
                    // 从其他表切入：切重击表，段位归 0（重击第一段）
                    Player.SkillCombo.UpdateSkillConfig(ComboSet.Heavy);
                }

                _isDistanceLocked = false;
                Attack();
                return true;
            }

            // 左键正常连招：按配置里的 nextAttackIndex 衔接
            case AttackInput.Normal when !Player.SkillCombo.IsCurrent(ComboSet.Heavy):
                // nextAttackIndex >= 0：跳到指定段（Attack04 配 0 即「跳过重击回第一段」）
                // nextAttackIndex == -1：自增到下一段（超过最后一段会自动回到第一段）
                Player.SkillCombo.CurAttackIndex = currentData.nextAttackIndex >= 0
                    ? currentData.nextAttackIndex
                    : Player.SkillCombo.CurAttackIndex + 1;

                _isDistanceLocked = false;
                CheckRushAttack();
                Attack();
                return true;

            default:
                return false;
        }
    }

    private void TransitionTo(AttackPhase phase) => _phase = phase;

    #region 顿帧

    // 顿帧：用栈式嵌套记录时间缩放，重叠顿帧时能正确逐层恢复，避免 _originalTimeScale 存错值
    public bool IsInHitStop => _hitStopStack.Count > 0; // 是否处于顿帧中

    private float _hitStopDuration; // 顿帧持续时间
    private float _hitStopEndTime; // 顿帧结束时间

    // 顿帧强度配置（可调整）
    [Header("顿帧配置")] private float _hitStopTimeScale; // 顿帧时的时间缩放（0=完全暂停，0.1=轻微慢放）
    private readonly float _hitStopDurationDefault = 0.08f; // 顿帧持续时间
    private float _animSpeedDuringHitStop; // 顿帧时动画速度（0=冻结帧）

    // 顿帧栈：每次 EnterHitStop 压入「进入前的 timeScale」，结束逐层弹栈恢复
    private readonly Stack<float> _hitStopStack = new();

    public void EnterHitStop(float duration = 0f, float timeScale = 0f)
    {
        // 避免重复触发顿帧
        AttackData data = CachedAttackData;
        if (data?.vfxDataList == null ||
            Player.SkillCombo.CurVFXIndex < 0 || Player.SkillCombo.CurVFXIndex >= data.vfxDataList.Count ||
            !data.vfxDataList[Player.SkillCombo.CurVFXIndex].isInHitStop)
            return;

        // 1. 压栈记录进入前的 timeScale（重叠顿帧时各层记住各层的旧值）
        _hitStopStack.Push(Time.timeScale);
        _hitStopDuration = duration <= 0 ? _hitStopDurationDefault : duration;
        float targetTimeScale = timeScale <= 0 ? _hitStopTimeScale : timeScale;
        // 2. 启动顿帧
        _hitStopEndTime = Time.unscaledTime + _hitStopDuration; // 用unscaledTime避免时间缩放影响
        // 3. 真顿帧：暂停全局时间（仅战斗相关，UI不受影响）
        Time.timeScale = targetTimeScale;
        // 4. 冻结动画：直接停在当前帧，而非慢放
        Player.playerModel.Animator.speed = _animSpeedDuringHitStop;
        Player.playerModel.Animator.Update(0); // 强制更新动画，冻结当前帧
    }

    // 顿帧处理专用方法
    private void HandleHitStop()
    {
        // 用unscaledTime判断顿帧是否结束（不受Time.timeScale影响）
        if (Time.unscaledTime >= _hitStopEndTime)
        {
            // 1. 弹栈恢复时间缩放：重叠顿帧时回到「上一层进入前的值」，最终恢复到初始值
            if (_hitStopStack.Count > 0) Time.timeScale = _hitStopStack.Pop();
            // 2. 恢复动画速度（立即恢复，保证跟手）
            Player.playerModel.Animator.speed = 1;
            // 3. 若栈未清空（仍有外层顿帧未结束），保持冻结由外层继续驱动
            if (_hitStopStack.Count > 0)
            {
                Player.playerModel.Animator.speed = _animSpeedDuringHitStop;
                Player.playerModel.Animator.Update(0);
                _hitStopEndTime = Time.unscaledTime + _hitStopDuration;
            }

            return;
        }

        // 顿帧期间：强制冻结动画+屏蔽所有输入
        Player.playerModel.Animator.Update(0);
    }

    #endregion

    #region 攻击和旋转

    private void Attack()
    {
        TransitionTo(AttackPhase.Active); // 起手新段：回到「攻击进行」阶段
        if (Player.SkillCombo.CurAttackIndex == -1) Player.SkillCombo.CurAttackIndex = 0;
        Player.SkillCombo.StartSkill();
    }

    private void HandRotate(float h, float v, bool hasInput)
    {
        AttackData data = CachedAttackData;

        // 当前段是否允许「无敌人时旋转」：有特效数据时读该段 canRotate，没有特效数据时默认允许旋转。
        // 旋转是攻击手感的基础，不应因特效配置缺失（如字段改名导致数据丢失）而失效。
        bool canRotate = true;
        if (data?.vfxDataList != null && Player.SkillCombo.CurVFXIndex >= 0 &&
            Player.SkillCombo.CurVFXIndex < data.vfxDataList.Count)
            canRotate = data.vfxDataList[Player.SkillCombo.CurVFXIndex].canRotate;

        if (!_isDistanceLocked && _targetEnemy is null && canRotate)
        {
            if (!hasInput) return;
            Vector3 input = new Vector3(h, 0, v);
            float y = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0, y, 0) * input;
            Player.playerModel.transform.rotation = Quaternion.Slerp
            (Player.playerModel.transform.rotation, Quaternion.LookRotation(moveDir),
                Player.rotationSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region 根运动

    // 原生根运动回调，仅做距离判断，面向敌人
    private void OnRootMotion(Vector3 rootMotion, Quaternion rootRot)
    {
        // 已锁定距离/冲刺杀：停止根运动位移，仅保留旋转
        if (_isDistanceLocked || _isRushAttack)
        {
            Player.playerModel.transform.rotation *= rootRot;
            return;
        }

        if (_targetEnemy == null)
        {
            rootMotion.y = 0;
            Player.CharacterController.Move(rootMotion);
            // 仅保留XZ轴旋转，清空Y轴旋转（防止角色倾斜/震动）
            Quaternion rot = rootRot;
            rot.x = 0;
            rot.z = 0;
            Player.playerModel.transform.rotation *= rot;
            return;
        }

        // 有敌人：计算与敌人距离，判断是否锁定
        Vector3 toEnemy = _targetEnemy.position - Player.transform.position;
        toEnemy.y = 0; // 忽略Y轴，保持水平检测
        float distanceToEnemy = toEnemy.magnitude;

        // 到达攻击范围：锁定距离+触发击退，停止根运动
        if (distanceToEnemy <= NormalAttackRange)
        {
            _isDistanceLocked = true;
            return;
        }

        // 未到范围：执行原生根运动  强制面向敌人
        rootMotion.y = 0;
        Player.CharacterController.Move(rootMotion);
        FaceToEnemy();
    }

    private void OnRootMotionNormal(Vector3 rootMotion, Quaternion rootRot)
    {
        Player.CharacterController.Move(rootMotion);
    }

    #endregion

    #region 冲刺杀

    private Coroutine _rushToEnemyCoroutine;

    // 起手段是不是配置里标的「冲刺杀」（AttackData.isRushAttack）
    private void CheckRushAttack()
    {
        if (CheckRushOrPowerAttack())
        {
            _isRushAttack = true;
            // 敌人在冲刺范围内：启动冲刺协程，跳过根运动
            if (_targetEnemy != null &&
                Vector3.Distance(Player.transform.position, _targetEnemy.position) <= RushAttackRange)
            {
                _rushToEnemyCoroutine = MonoManager.Instance.StartCoroutine(RushToEnemyCoroutine());
            }
            else
            {
                Player.playerModel.SetRootMotionAction(OnRootMotionNormal);
            }
        }
    }

    // 冲刺杀协程：位移到敌人锁定距离，触发击退
    private IEnumerator RushToEnemyCoroutine()
    {
        Player.playerModel.ClearRootMotionAction(); // 关闭根运动，避免冲突
        while (_targetEnemy != null &&
               Vector3.Distance(Player.transform.position, _targetEnemy.position) > LockAttackDistance)
        {
            Vector3 dir = (_targetEnemy.position - Player.transform.position).normalized;
            dir.y = 0;
            Player.CharacterController.Move(dir * (RushMoveSpeed * Time.deltaTime));
            FaceToEnemy(); // 冲刺中始终面向敌人
            yield return null;
        }

        // 冲刺到位：锁定距离 + 触发击退
        if (_targetEnemy != null)
        {
            _isDistanceLocked = true;
        }
    }

    // 是不是配置里标了「冲刺杀」的起手段。
    // 这里用「意图段位」CurAttackIndex 判断（此时尚未起手，意图 == 即将播放的段），
    // 而非播放快照 PlayingIndex（要等 StartSkill 才更新，时序上赶不及 CheckRushAttack 前置调用）。
    private bool CheckRushOrPowerAttack()
    {
        return Player.SkillCombo.CurAttackIndex == 0 && Player.SkillCombo.CurrentAttackData?.isRushAttack == true;
    }

    #endregion

    #region 强制面向敌人,保持与敌人的锁定攻击距离,标签检测最近的敌人

    // 强制面向敌人
    private void FaceToEnemy()
    {
        if (_targetEnemy == null) return;
        Vector3 toEnemy = _targetEnemy.position - Player.transform.position;
        toEnemy.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(toEnemy);
        Player.playerModel.transform.rotation = Quaternion.Slerp(
            Player.playerModel.transform.rotation, targetRot, Time.deltaTime * 20f);
    }

    // 保持与敌人的锁定攻击距离
    private void KeepLockDistance()
    {
        Vector3 toEnemy = _targetEnemy.position - Player.transform.position;
        toEnemy.y = 0;
        // 计算目标位置：敌人位置 - 朝向角色的单位向量 * 锁定距离
        Vector3 targetPos = _targetEnemy.position - toEnemy.normalized * LockAttackDistance;
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
            if (dis < minDistance && dis <= AttackDetectRange)
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
        TransitionTo(AttackPhase.Active); // 重置阶段，避免下次进入残留 Recovery
        Player.SkillCombo.CurAttackIndex = 0;
        Player.SkillCombo.ClearAttackInput(); // 离开攻击状态时清掉残留的缓冲按键
        if (_rushToEnemyCoroutine != null)
        {
            MonoManager.Instance.StopCoroutine(_rushToEnemyCoroutine);
            _rushToEnemyCoroutine = null;
        }

        Player.playerModel.ClearRootMotionAction();
    }
}