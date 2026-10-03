using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerModel : MonoBehaviour
{
    // 持有玩家控制器引用（用于获取当前状态）
    [SerializeField] private PlayerCtrl player;
    //拿到动画控制器
    [SerializeField] private Animator playerAnimator;
    public Animator Animator => playerAnimator; //可以拿到动画控制器
    private AudioSource audioSource;

    // 拿到技能拥有者 
    private ISkillOwner skillOwner;
    [Header("武器列表")]
    [SerializeField] private WeaponController[] weapons;

    public void Init(ISkillOwner skillOwner)
    {
        if (!player.IsLocalPlayer) return;
        audioSource = player.GetComponent<AudioSource>();
        this.skillOwner = skillOwner;
    }

    #region 音效相关

    // 脚步声
    private void PlayFootSound()
    {
        if (!player.IsLocalPlayer) return; 
        audioSource.PlayOneShot(SoundManager.Instance.footSound);
    }
    // 收脚的步声
    private void PlayFootBackSound()
    {
        if (!player.IsLocalPlayer) return;
        audioSource.PlayOneShot(SoundManager.Instance.footBackSound);
    }
    // 收剑
    public void PlayWeaponBackSound() 
    {
        if (!player.IsLocalPlayer) return; 
        audioSource.PlayOneShot(SoundManager.Instance.weaponBackSound);
    }
    // 结束收剑
    public void PlayWeaponEndSound()
    {
        if (!player.IsLocalPlayer) return; 
        audioSource.PlayOneShot(SoundManager.Instance.weaponEndSound);
    }

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
        rootMotionAction = null;
    }

    /// <summary>
    /// 开启跟运动方法 一帧一帧执行
    /// </summary>
    private void OnAnimatorMove()
    {
        //Animator.deltaPosition是相对于上一帧偏移的位置，Animator.deltaRotation是相对于上一帧偏移的
        rootMotionAction?.Invoke(Animator.deltaPosition, Animator.deltaRotation);
    }

    #endregion
    #region 技能相关

    private float lastTime;

    public void StartSkillHit(int weaponIndex = 0)
    {
        if (!player.IsLocalPlayer) return;
        skillOwner.StartSkillHit(weaponIndex);
        //执行武器层伤害开始的的方法
        if (weaponIndex < weapons.Length && weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].StartSkillHit();
        }
        if (!(Random.value >= 0.5f) || Time.time - lastTime <= 1f) return;
        int speakIndex = Random.Range(0, SoundManager.Instance.playerAttackSpeaks.Count);
        audioSource.PlayOneShot(SoundManager.Instance.playerAttackSpeaks[speakIndex]);
        lastTime = Time.time;
    }

    public void StopSkillHit(int weaponIndex = 0)
    {
        if (!player.IsLocalPlayer) return;
        skillOwner.StopSkillHit(weaponIndex);
        if (weaponIndex < weapons.Length && weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].StopSkillHit();
        }
    }

    public void SkillCanSwitch()
    {
        if (!player.IsLocalPlayer) return;
        skillOwner.SkillCanSwitch();
    }

    #endregion
    
}
