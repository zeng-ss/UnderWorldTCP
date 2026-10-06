using Unity.Cinemachine;
using UnityEngine;

// 相机绑定子系统：查找场景虚拟相机并绑定 Follow / LookAt（原 PlayerCtrl.Init() 的相机部分）。
// PinCamera 由拼刀流程（输入子系统）在拼刀时临时激活。
public class PlayerCameraBinder
{
    public Transform CameraTransform { get; private set; }
    public CinemachineVirtualCamera VirtualCameraEx { get; private set; }

    // 拼刀特写相机（TryPin 时激活，1 秒后关闭）
    public CinemachineVirtualCamera PinCamera { get; private set; }

    private CinemachineFreeLook _freeLook;

    /// <summary>Start 阶段调用：找相机并绑定跟随目标</summary>
    public void Bind(Transform player, Transform playerModel)
    {
        CameraTransform = Camera.main?.transform;
        PinCamera = GameObject.Find("VirtualCamera_Pin")?.GetComponent<CinemachineVirtualCamera>();
        VirtualCameraEx = GameObject.Find("VirtualCamera_Ex")?.GetComponent<CinemachineVirtualCamera>();
        _freeLook = Object.FindAnyObjectByType<CinemachineFreeLook>();
        Transform lookAtTarget = playerModel.Find("LookAt");
        if (lookAtTarget)
        {
            _freeLook.Follow = lookAtTarget;
            _freeLook.LookAt = lookAtTarget;
            if (PinCamera)
            {
                PinCamera.Follow = player;
                PinCamera.LookAt = player;
                PinCamera.gameObject.SetActive(false);
            }

            if (VirtualCameraEx)
            {
                VirtualCameraEx.Follow = lookAtTarget;
                VirtualCameraEx.LookAt = lookAtTarget;
                VirtualCameraEx.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("找不到 lookAt 子物体，请检查角色层级");
        }
    }
}