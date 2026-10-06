using UnityEngine;

public class PlayerDefenceState : PlayerState
{
    private enum DefenceChildState
    {
        Enter, //进入格挡
        Hold, //一直按着 F不松手，一直处于格挡状态
        CounterAttack, //反击，动画结束回到 Idle状态
        Exit //退出格挡
    }

    private DefenceChildState _defenceState;

    private DefenceChildState DefenceState
    {
        get => _defenceState;
        set
        {
            _defenceState = value;
            switch (_defenceState)
            {
                case DefenceChildState.Enter:
                    Player.PlayAnimation("EnterDefence");
                    break;
                case DefenceChildState.Hold:
                    Player.PlayAnimation("HoldDefence");
                    break;
                case DefenceChildState.CounterAttack:
                    //_player.playerModel.transform.LookAt(((Component)_player.Enemy).transform); //面向敌人
                    if (!Player.IsLocalPlayer) return;
                    // 本地先更新（预测，提升手感）
                    Player.UpdateSkillConfig(1);
                    Player.CurAttackIndex = 1;
                    Player.StartSkill(Player.CurSkillConfig.skillConfigs[Player.CurAttackIndex]);
                    break;
                case DefenceChildState.Exit:
                    Player.PlayAnimation("ExitDefence");
                    break;
            }
        }
    }

    public override void Enter()
    {
        Player.playerModel.SetRootMotionAction(OnRootMotion);
        DefenceState = DefenceChildState.Enter;
    }

    private void OnRootMotion(Vector3 arg1, Quaternion arg2)
    {
        Player.CharacterController.Move(arg1);
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        Player.playerModel.ClearRootMotionAction();
    }
}