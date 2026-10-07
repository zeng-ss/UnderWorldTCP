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
    private AudioSource _audioSource;

    // 拿到技能拥有者 
    private ISkillOwner _skillOwner;
    [Header("武器列表")] [SerializeField] private WeaponController[] weapons;

    public void Init(ISkillOwner skillOwner)
    {
        if (!player.Core.IsLocalPlayer) return;
        _audioSource = player.GetComponent<AudioSource>();
        this._skillOwner = skillOwner;
    }

    #region 音效相关

    // 脚步声
    private void PlayFootSound()
    {
        if (!player.Core.IsLocalPlayer) return;
        _audioSource.PlayOneShot(AppContext.Sound.FootSound);
    }

    // 收脚的步声
    private void PlayFootBackSound()
    {
        if (!player.Core.IsLocalPlayer) return;
        _audioSource.PlayOneShot(AppContext.Sound.FootBackSound);
    }

    // 收剑
    public void PlayWeaponBackSound()
    {
        if (!player.Core.IsLocalPlayer) return;
        _audioSource.PlayOneShot(AppContext.Sound.WeaponBackSound);
    }

    // 结束收剑
    public void PlayWeaponEndSound()
    {
        if (!player.Core.IsLocalPlayer) return;
        _audioSource.PlayOneShot(AppContext.Sound.WeaponEndSound);
    }

    #endregion

    #region 根运动

    private Action<Vector3, Quaternion> _rootMotionAction;

    /// <summary>设置根运动回调</summary>
    public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
    {
        this._rootMotionAction = rootMotionAction;
    }

    /// <summary>
    /// 清除跟运动
    /// </summary>
    public void ClearRootMotionAction()
    {
        _rootMotionAction = null;
    }

    // 开启跟运动方法 一帧一帧执行
    private void OnAnimatorMove()
    {
        //Animator.deltaPosition是相对于上一帧偏移的位置，Animator.deltaRotation是相对于上一帧偏移的
        _rootMotionAction?.Invoke(Animator.deltaPosition, Animator.deltaRotation);
    }

    #endregion

    #region 技能相关

    private float _lastTime;

    public void StartSkillHit(int weaponIndex = 0)
    {
        if (!player.Core.IsLocalPlayer) return;
        _skillOwner.StartSkillHit(weaponIndex);
        //执行武器层伤害开始的的方法
        if (weaponIndex < weapons.Length && weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].StartSkillHit();
        }

        if (!(Random.value >= 0.5f) || Time.time - _lastTime <= 1f) return;
        // 攻击语音列表未配置（null 或空）时跳过，避免 NRE / 越界
        if (AppContext.Sound.PlayerAttackSpeaks is not { Count: > 0 }) return;
        int speakIndex = Random.Range(0, AppContext.Sound.PlayerAttackSpeaks.Count);
        _audioSource.PlayOneShot(AppContext.Sound.PlayerAttackSpeaks[speakIndex]);
        _lastTime = Time.time;
    }

    public void StopSkillHit(int weaponIndex = 0)
    {
        if (!player.Core.IsLocalPlayer) return;
        _skillOwner.StopSkillHit(weaponIndex);
        if (weaponIndex < weapons.Length && weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].StopSkillHit();
        }
    }

    public void SkillCanSwitch()
    {
        if (!player.Core.IsLocalPlayer) return;
        _skillOwner.SkillCanSwitch();
    }

    #endregion
}