using UnityEngine;

namespace HotUpdate.Player
{
    public class PlayerCameraBinder
    {
        public Transform CameraTransform { get; private set; }

        public PlayerCameraRig Rig { get; private set; }

        public void Bind(Transform player, Transform playerModel)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[PlayerCameraBinder] 场景里找不到主相机（Tag = MainCamera）");
                return;
            }

            CameraTransform = cam.transform;
            Rig = cam.GetComponent<PlayerCameraRig>();
            Transform lookAt = playerModel.Find("LookAt");
            if (lookAt == null)
            {
                Debug.LogWarning("[PlayerCameraBinder] 角色模型上找不到 LookAt 子物体，改用模型根节点作为跟随点");
                lookAt = playerModel;
            }

            Rig.SetTarget(lookAt, player);
        }
    }
}