using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerIdleState : PlayerState
{
    public override void Enter()
    {
        Player.PlayAnimation("Idle");
    }

    public override void Update()
    {
        if (!Player.Core.IsLocalPlayer) return;
        // 原先靠判断聊天框是否聚焦来屏蔽输入，现在统一由 InputManager 的输入锁接管
        if (!InputManager.Instance.IsGameplayInputEnabled) return;
        if (InputManager.IsPointerOverBlockingUI()) return;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0 || v != 0)
        {
            Player.StateMachine.ChangeTo(PlayerStateType.Move);
        }
        // 攻击 / 闪避入口已统一由 PlayerInputHandler 走 InputManager 处理，这里不再读键
    }

    public override void Exit()
    {
    }
}