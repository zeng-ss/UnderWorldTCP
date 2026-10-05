using System;

public class PlayerStateMachine
{
    private readonly PlayerCore _core;

    public State_Machine Machine { get; } = new();

    public PlayerStateType CurrentState { get; private set; }
    public PlayerStateType LastState { get; private set; }

    public PlayerStateMachine(PlayerCore core) => _core = core;

    // 状态机绑定宿主
    public void Init(IState_MachineOwner owner) => Machine.Init(owner);

    public void ChangeTo(PlayerStateType stateType, bool isRefreshState = false)
    {
        if (_core.IsLock) return;
        if (CurrentState != stateType)
        {
            LastState = CurrentState;
        }

        CurrentState = stateType;
        switch (stateType)
        {
            case PlayerStateType.Idle: Machine.ChangeState<PlayerIdleState>(isRefreshState); break;
            case PlayerStateType.Move: Machine.ChangeState<PlayerMoveState>(isRefreshState); break;
            case PlayerStateType.Attack: Machine.ChangeState<PlayerAttackState>(isRefreshState); break;
            case PlayerStateType.Evade: Machine.ChangeState<PlayerEvadeState>(isRefreshState); break;
            case PlayerStateType.Dead: Machine.ChangeState<PlayerDeadState>(isRefreshState); break;
            case PlayerStateType.Hurt: Machine.ChangeState<PlayerHurtState>(isRefreshState); break;
            case PlayerStateType.EX: Machine.ChangeState<ExAttackState>(isRefreshState); break;
            default: throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
        }
    }
}