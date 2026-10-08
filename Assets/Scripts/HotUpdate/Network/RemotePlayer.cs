// 远程玩家组件：挂载在其他玩家的GameObject上，处理位置平滑插值

using UnityEngine;
using UnityEngine.Serialization;

public class RemotePlayer : MonoBehaviour
{
    public int RoleId { get; private set; }
    public string Nickname { get; private set; }
    [FormerlySerializedAs("TargetPos")] public Vector3 targetPos;

    [FormerlySerializedAs("TargetRotation")]
    public Quaternion targetRotation;

    public float smoothSpeed = 10f;
    private Transform _modelTransform;
    public PlayerCtrl Ctrl { get; private set; }

    public void Init(int roleId, string nickname, Vector3 pos)
    {
        RoleId = roleId;
        Nickname = nickname;
        targetPos = pos;
        targetRotation = Quaternion.identity;
        Ctrl = GetComponent<PlayerCtrl>();

        // 找模型子节点（Character 预制体的 root/playerModel）
        _modelTransform = GetComponentInChildren<PlayerModel>().transform;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        Transform rotTarget = _modelTransform ? _modelTransform : transform;
        rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}