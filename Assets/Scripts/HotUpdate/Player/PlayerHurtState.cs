using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Player
{
    public class PlayerHurtState : PlayerState
    {
        public override void Enter()
        {
            if (!Player.Core.IsLocalPlayer) return;
            Player.playerModel.SetRootMotionAction(OnRootMotion);
            Player.PlayAnimation("Hurt");
        }

        private void OnRootMotion(Vector3 arg1, Quaternion arg2)
        {
            Player.CharacterController.Move(arg1);
        }

        public override void Update()
        {
            if (!Player.Core.IsLocalPlayer) return;
            if (IsAnimationMoreThanTime("Hurt", 0.5f))
            {
                Player.StateMachine.ChangeTo(PlayerStateType.Idle);
            }
        }

        public override void Exit()
        {
        }
    }
}
