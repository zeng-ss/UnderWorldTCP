using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerIdleState : Player_State
{
    public override void Enter()
    {
        _player.PlayAnimation("Idle");
    }

    public override void Update()
    {
        if (!_player.IsLocalPlayer) return;
        if (_player.chatPanel && _player.chatPanel.chatInput.isFocused) return;
        if (GameManager.Instance.IsPointerOverSpecificUILayer(LayerMask.GetMask("LockInput"))) return;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0 || v != 0)
        {
            _player.ChangeState(PlayerStateType.Move);
        }
        if (Input.GetKeyDown(KeyCode.LeftShift)) _player.ChangeState(PlayerStateType.Evade);
        if (Input.GetKeyDown(KeyCode.Mouse0) 
            || (Input.GetKeyDown(KeyCode.Mouse1) && _player.curSkillConfig == _player.skillConfigList[2]))
        {
            _player.ChangeState(PlayerStateType.Attack);
        }
    }

    public override void Exit()
    {
        
    }
    
}