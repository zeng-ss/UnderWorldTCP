using System;
using UnityEngine;

public class EnemyModel : MonoBehaviour
{
    // 持有玩家控制器引用（用于获取当前状态）
    [SerializeField] private EnemyCtrl enemy;
    //拿到动画控制器
    [SerializeField] private Animator enemyAnimator;
    public Animator Animator => enemyAnimator; //可以拿到动画控制器
    private AudioSource audioSource;

    // 拿到技能拥有者 
    private ISkillOwner skillOwner;
    [Header("武器列表")]
    [SerializeField] private WeaponController[] weapons;

    public void Init(ISkillOwner skillOwner)
    {
        audioSource = enemy.GetComponent<AudioSource>();
        this.skillOwner = skillOwner;
    }

    #region 音效相关

    // 脚步声
    private void PlayFootSound() { if (!enemy.IsServer) return; audioSource.PlayOneShot(AppContext.Sound.footSound); }
    // 收脚的步声
    private void PlayFootBackSound() { if (!enemy.IsServer) return; audioSource.PlayOneShot(AppContext.Sound.footBackSound); }
    // 收剑
    public void PlayWeaponBackSound() { if (!enemy.IsServer) return; audioSource.PlayOneShot(AppContext.Sound.weaponBackSound); }
    // 结束收剑
    public void PlayWeaponEndSound() { if (!enemy.IsServer) return; audioSource.PlayOneShot(AppContext.Sound.weaponEndSound); }

    #endregion
    #region 根运动

    private Action<Vector3, Quaternion> rootMotionAction;

    /// <summary>
    /// 设置跟运动
    /// </summary>
    /// <param name="rootMotionAction"></param>
    public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
    {
        this.rootMotionAction = rootMotionAction;
    }
    /// <summary>
    /// 清除跟运动
    /// </summary>
    public void ClearRootMotionAction()
    {
        this.rootMotionAction = null;
    }

    /// <summary>
    /// 开启跟运动方法 一帧一帧执行
    /// </summary>
    private void OnAnimatorMove()
    {
        //Animator.deltaPosition是相对于上一帧偏移的位置，Animator.deltaRotation是相对于上一帧偏移的
        this.rootMotionAction?.Invoke(Animator.deltaPosition, Animator.deltaRotation);
    }

    #endregion
    #region 技能相关
    
    public void StartPin(int isStartPin = 0)
    {
        if (!enemy.IsServer || enemy.isPinFinished) return;
        enemy.isStartPin = isStartPin == 1;
    }
    
    public void StartSkillHit(int weaponIndex = 0)
    {
        if (!enemy.IsServer) return;
        skillOwner.StartSkillHit(weaponIndex);
        //执行武器层伤害开始的的方法
        if (weaponIndex < weapons.Length && weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].StartSkillHit();
        }
    }

    public void StopSkillHit(int weaponIndex = 0)
    {
        if (!enemy.IsServer) return;
        skillOwner.StopSkillHit(weaponIndex);
        if (weaponIndex < weapons.Length && weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].StopSkillHit();
        }
        enemy.isStartPin = false;
    }

    public void CanBeHurt()
    {
        if (!enemy.IsServer) return;
        enemy.isStartLock = false;
        enemy.isCanPlayHurtAni = true;
    }

    #endregion
    
}
