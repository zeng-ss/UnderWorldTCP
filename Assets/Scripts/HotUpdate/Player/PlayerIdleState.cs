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
        if (!Player.IsLocalPlayer) return;
        // 原先靠判断聊天框是否聚焦来屏蔽输入，现在统一由 InputManager 的输入锁接管
        if (!InputManager.Instance.IsGameplayInputEnabled) return;
        if (InputManager.IsPointerOverBlockingUI()) return;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0 || v != 0)
        {
            Player.ChangeState(PlayerStateType.Move);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift)) Player.ChangeState(PlayerStateType.Evade);
        if (Input.GetKeyDown(KeyCode.Mouse0)
            || (Input.GetKeyDown(KeyCode.Mouse1) && Player.CurSkillConfig == Player.SkillConfigList[2]))
        {
            Player.ChangeState(PlayerStateType.Attack);
        }
    }

    public override void Exit()
    {
    }
}