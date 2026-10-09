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

            // 等待动画播放完成
            if (IsAnimationMoreThanTime("Evade", 0.5f))
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Idle);
                return;
            }

            if (IsAnimationMoreThanTime("Evade", 0.3f))
            {
                if (h != 0 || v != 0)
                {
                    Player.StateMachine.ChangeTo(PlayerStateType.Move);
                }
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
