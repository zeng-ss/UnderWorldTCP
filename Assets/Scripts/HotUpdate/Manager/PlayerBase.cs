using UnityEngine;

public abstract class PlayerBase : MonoBehaviour
{
    // 通用属性
    public float Health { get; set; } = 100f;
    public float Stamina { get; set; } = 100f;
    public Vector3 Position => transform.position;

    // 通用组件
    protected CharacterController CharacterController;
    protected Animator Animator;
    protected Camera PlayerCamera;

    // 通用方法（所有角色都有）
    public virtual void Initialize()
    {
        CharacterController = GetComponent<CharacterController>();
        Animator = GetComponentInChildren<Animator>();
    }

    // 通用工具方法
    protected void PlayAnimation(string animationName)
    {
        if (Animator != null)
            Animator.Play(animationName);
    }
}