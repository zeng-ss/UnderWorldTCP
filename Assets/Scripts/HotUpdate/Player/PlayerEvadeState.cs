using HotUpdate.Core;
using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Player
{
    public class PlayerEvadeState : PlayerState
    {
        public override void Enter()
        {
            if (!Player.Core.IsLocalPlayer) return;
            if (InputManager.IsPointerOverBlockingUI())
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Idle);
                return;
            }

            Player.playerModel.SetRootMotionAction(OnRootMation);
            Player.PlayAnimation("Evade");
        }

        private void OnRootMation(Vector3 arg1, Quaternion arg2)
        {
            Player.CharacterController.Move(arg1);
        }

        public override void Update()
        {
            // 只允许本地玩家执行逻辑
            if (!Player.Core.IsLocalPlayer) return;
            if (!InputManager.Instance.IsGameplayInputEnabled) return;
            if (InputManager.IsPointerOverBlockingUI())
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Idle);
                return;
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            // 闪避位移靠根运动驱动，位移在动画后半段：
            // 原来播到 50% 就切 Idle，等于只播了个起手动作，看不到位移。这里改成等它播完。
            if (IsAnimationFinished("Evade"))
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Idle);
                return;
            }

            // 播到 30% 之后有移动输入：直接接奔跑
            if (IsAnimationMoreThanTime("Evade", 0.3f) && (h != 0 || v != 0))
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Move);
            }
        }

        // 闪避中再次按 Shift 连闪：由 PlayerInputHandler 在已是 Evade 状态时调用
        public void RefreshEvade()
        {
            Player.PlayAnimation("Evade");
        }

        public void SetEvade(bool isShiftDoubleTap)
        {
            Player.playerModel.Animator.SetFloat("EvadeDirection", isShiftDoubleTap ? 1 : 0);
        }

        public override void Exit()
        {
            Player.playerModel.ClearRootMotionAction();
        }
    }
}
