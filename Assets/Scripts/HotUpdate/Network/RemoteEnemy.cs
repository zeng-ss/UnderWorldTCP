using HotUpdate.Enemy;
using UnityEngine;

namespace HotUpdate.Network
{
    // 远程敌人组件：挂载在其他敌人的GameObject上，处理位置平滑插值


    public class RemoteEnemy : MonoBehaviour
    {
        public int RoleId { get; private set; }
        public Vector3 targetPos;
        public Quaternion targetRotation;
        public float smoothSpeed = 10f;
        private Transform _modelTransform;

        public void Init(int roleId, int serverInstanceId, Vector3 pos)
        {
            RoleId = roleId;
            targetPos = pos;
            targetRotation = Quaternion.identity;
            GetComponent<EnemyCtrl>();

            // 找模型子节点（Character 预制体的 root/playerModel）
            _modelTransform = GetComponentInChildren<EnemyModel>().transform;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
            Transform rotTarget = _modelTransform ? _modelTransform : transform;
            rotTarget.rotation = Quaternion.Slerp(rotTarget.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
    }
}
