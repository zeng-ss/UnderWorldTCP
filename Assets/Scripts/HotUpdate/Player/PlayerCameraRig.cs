using System.Collections.Generic;
using HotUpdate.Core;
using UnityEngine;

namespace HotUpdate.Player
{
    public class PlayerCameraRig : MonoBehaviour
    {
        // 相机状态
        private enum CameraMode
        {
            Normal = 0,
            Pin = 1,
            Ex = 2,
        }

        #region 可调参数 —— 正常视角

        [Header("① 正常 · 鼠标视角")] [SerializeField]
        private float lookSensitivity = 2.4f;

        [Tooltip("勾上：鼠标向下移动时相机向下移动（本项目默认手感，对应 UE 的 bInvertLookY）")] [SerializeField]
        private bool invertLookY = true;

        [SerializeField] private bool enableMouseSmoothing = true;

        [Tooltip("滑动平均的采样帧数，越大越顺滑但越迟钝")] [SerializeField]
        private int mouseSmoothingSamples = 3;

        [Tooltip("单帧鼠标位移超过该值时视为快速甩视角，直接使用原始值不做平滑")] [SerializeField]
        private float mouseSmoothThreshold = 0.6f;

        [SerializeField] private float pitchMin = -32f;
        [SerializeField] private float pitchMax = 72f;
        [SerializeField] private float defaultPitch = 14f;

        [Header("① 正常 · 弹簧臂 SpringArm")] [Tooltip("相机跟随点相对角色模型挂点的偏移（挂在 LookAt 上）")] [SerializeField]
        private Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);

        [Tooltip("肩位偏移：相机相对跟随点的横向/纵向拉偏，0 = 正后方")] [SerializeField]
        private Vector3 socketOffset = Vector3.zero;

        [SerializeField] private float targetArmLength = 6.5f;
        [SerializeField] private float minArmLength = 2.2f;
        [SerializeField] private float maxArmLength = 12f;
        [SerializeField] private float zoomSpeed = 2.2f;
        [SerializeField] private float pivotFollowSmoothTime = 0.03f;

        [Header("① 正常 · 相机滞后 CameraLag")] [SerializeField]
        private bool enableCameraLocationLag = true;

        [Tooltip("相机位置逼近理想位置的速度，越小越「拖」")] [SerializeField]
        private float cameraLagSpeed = 14f;

        [Tooltip("滞后距离上限：超过就直跟（防止传送 / 冲刺时相机被远远甩掉），0 = 不限")] [SerializeField]
        private float cameraLagMaxDistance = 3.5f;

        [SerializeField] private bool enableCameraRotationLag;
        [SerializeField] private float cameraRotationLagSpeed = 18f;

        [Header("① 正常 · 碰撞")] [SerializeField] private bool doCollisionTest = true;

        [Tooltip("探测球半径，越大越不容易穿模，但贴墙时镜头会被推得越近")] [SerializeField]
        private float probeSize = 0.3f;

        [SerializeField] private float collisionPadding = 0.1f;

        #endregion

        #region 可调参数 —— 拼刀特写

        [Header("② 拼刀特写 Pin")]
        [Tooltip("镜头绕「玩家→对手」连线的偏移角：0 = 玩家背后过肩看对手，±90 = 正侧拍，180 = 对手背后看玩家。\n" +
                 "正负号决定绕到玩家的哪一侧（左手 / 右手）。")]
        [SerializeField]
        private float pinYawOffset = -72f;

        [Tooltip("正值 = 相机高于锚点（俯视），负值 = 低于锚点（仰拍）")] [SerializeField]
        private float pinPitch = 10f;

        [Tooltip("相机到「两人中点」的距离（= 弹簧臂臂长）")] [SerializeField]
        private float pinDistance = 3.4f;

        [Tooltip("锚点相对两人中点的高度")] [SerializeField]
        private float pinHeight = 1.25f;

        [SerializeField] private float pinFov = 45f;
        [SerializeField] private float pinBlendTime = 0.18f;

        #endregion

        #region 可调参数 —— 大招特写

        [Header("③ 大招特写 Ex")] [Tooltip("镜头绕角色朝向的偏移角：0 = 角色背后，180 = 角色正面，±120 左右 = 侧前方")] [SerializeField]
        private float exYawOffset = 145f;

        [Tooltip("负值 = 相机低于锚点（仰拍，显气势）")] [SerializeField]
        private float exPitch = -6f;

        [SerializeField] private float exDistance = 4.2f;
        [SerializeField] private float exPivotHeight = 1.3f;
        [SerializeField] private float exFov = 30f;
        [SerializeField] private float exBlendTime = 0.22f;

        [Tooltip("勾上：进入大招时锁定角色朝向作为基准（角色转身时镜头不跟着甩）")] [SerializeField]
        private bool exLockBaseYaw = true;

        #endregion

        #region 可调参数 —— 遮挡淡出

        [Header("④ 遮挡淡出（相机与角色之间挡视线的物体变透明）")] [SerializeField]
        private bool enableObstacleFade = true;

        [Tooltip("被遮挡时淡到多少透明度：1 = 不处理，0 = 完全透明")] [SerializeField] [Range(0f, 1f)]
        private float obstacleFadeAlpha = 0.3f;

        [Tooltip("淡出 / 恢复的速度，越大越干脆")] [SerializeField]
        private float obstacleFadeSpeed = 9f;

        [Tooltip("参与淡出的层；勾满 = 全部参与（角色自身始终排除）")] [SerializeField]
        private LayerMask obstacleFadeLayers = ~0;

        [Tooltip("淡出用的 Shader，留空则自动找 UnderWord/CameraObstacleFade")] [SerializeField]
        private Shader obstacleFadeShader;

        [SerializeField] private bool debugDrawObstacleFade;

        #endregion

        [Header("震屏")] [SerializeField] private float shakeScale = 0.32f;
        [SerializeField] private float shakeDuration = 0.28f;

        private const string MouseXAxis = "Mouse X";
        private const string MouseYAxis = "Mouse Y";
        private const string MouseScrollAxis = "Mouse ScrollWheel";

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];

        private Camera _camera;
        private Transform _pivotTarget; // 正常视角的跟随目标（角色模型的 LookAt 子物体）
        private Transform _ownerRoot; // 角色根节点（排除自身碰撞体；特写机位也相对它计算）
        private Transform _facingSource; // 角色朝向来源（模型挂点：root 的 forward 不跟着转身变）

        private float _yaw;
        private float _pitch;
        private float _targetArmLength;
        private Vector3 _smoothedPivot;
        private Vector3 _pivotVelocity;
        private Vector3 _laggedLocation; // 带滞后后的相机位置
        private Vector2 _smoothedMouseDelta;
        private bool _armReady;

        private bool _hasTarget;

        private float _baseFov = 40f;

        // 状态机：当前状态、过渡来源状态、过渡进度（0→1）
        private CameraMode _mode = CameraMode.Normal;
        private CameraMode _fromMode = CameraMode.Normal;
        private float _modeBlend = 1f;
        private Transform _pinFocus; // 拼刀对手
        private float _lockedBaseYaw; // 大招进入时锁定的基准朝向

        private float _shakeTimer;
        private float _shakeAmp;

        #region 生命周期

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[PlayerCameraRig] 场景里找不到相机，无法接管。");
                enabled = false;
                return;
            }

            _baseFov = _camera.fieldOfView;
            _targetArmLength = targetArmLength;

            /*
            // 按类型名关掉 CinemachineBrain：本脚本不引用 Cinemachine 程序集，去掉这套依赖
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null) continue;
                if (behaviour.GetType().Name == "CinemachineBrain") behaviour.enabled = false;
            }
            */

            DisableSceneObject("FreeLook Camera");
            DisableSceneObject("VirtualCamera_Pin");
            DisableSceneObject("VirtualCamera_Ex");
        }

        private static void DisableSceneObject(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go != null) go.SetActive(false);
        }

        // 关闭 / 销毁时把还在半透明的障碍物还原，否则会留下一地半透明墙
        private void OnDisable()
        {
            ClearObstacleFade();
        }

        private void OnDestroy()
        {
            ClearObstacleFade();
            if (_fadeMaterial != null)
            {
                Destroy(_fadeMaterial);
                _fadeMaterial = null;
            }
        }

        #endregion

        /// <summary>绑定跟随目标。pivot 用角色模型上的 LookAt 子物体，ownerRoot 用角色根节点（排除自身碰撞）</summary>
        public void SetTarget(Transform pivot, Transform ownerRoot)
        {
            if (_camera == null) return;

            _pivotTarget = pivot != null ? pivot : ownerRoot;
            _ownerRoot = ownerRoot;
            if (_pivotTarget == null) return;

            // 朝向来源用模型挂点：转身（移动 / 面向敌人）转的是 playerModel，root 的 forward 不跟着变
            _facingSource = _pivotTarget;

            // 初始朝向对齐角色正面，避免第一帧镜头猛地甩一下
            Vector3 forward = _facingSource.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            _yaw = Quaternion.LookRotation(forward.normalized).eulerAngles.y;

            _pitch = Mathf.Clamp(defaultPitch, pitchMin, pitchMax);
            _smoothedPivot = GetRawPivot();
            _pivotVelocity = Vector3.zero;
            _smoothedMouseDelta = Vector2.zero;
            _armReady = true;

            _mode = CameraMode.Normal;
            _fromMode = CameraMode.Normal;
            _modeBlend = 1f;
            _pinFocus = null;
            _lockedBaseYaw = _yaw;

            _hasTarget = true;
            CameraPose initialPose = ComputePose(_mode);
            _laggedLocation = ComputeIdealLocation(initialPose);
            UpdateArm(0f, initialPose);
        }

        #region 外部指令（状态切换）

        public void Shake(float strength)
        {
            if (strength <= 0f) return;
            _shakeAmp = Mathf.Max(_shakeAmp, strength);
            _shakeTimer = shakeDuration;
        }

        /// <summary>拼刀特写：镜头切到「玩家与对手之间」的侧面近景</summary>
        public void EnterPin(Transform focus)
        {
            ChangeMode(CameraMode.Pin, focus);
        }

        /// <summary>退出拼刀特写，回到正常视角</summary>
        public void ExitPin()
        {
            if (_mode == CameraMode.Pin) ChangeMode(CameraMode.Normal, null);
        }

        /// <summary>大招特写：绕到角色侧前方 + 收窄 FOV</summary>
        public void EnterEx()
        {
            ChangeMode(CameraMode.Ex, null);
        }

        /// <summary>退出大招特写，回到正常视角</summary>
        public void ExitEx()
        {
            if (_mode == CameraMode.Ex) ChangeMode(CameraMode.Normal, null);
        }

        // 状态切换：记下过渡起点，之后由 UpdateModeBlend 按各自的 BlendTime 混到新机位
        private void ChangeMode(CameraMode mode, Transform pinFocus)
        {
            if (_mode == mode)
            {
                if (mode == CameraMode.Pin) _pinFocus = pinFocus;
                return;
            }

            // 上一次过渡还没走完又切状态时，过渡起点要接回最初的来源状态
            _fromMode = _modeBlend >= 1f ? _mode : _fromMode;
            _mode = mode;
            _modeBlend = 0f;

            if (mode == CameraMode.Pin) _pinFocus = pinFocus;
            if (mode == CameraMode.Ex) _lockedBaseYaw = GetOwnerFlatRotation().eulerAngles.y;
        }

        #endregion

        private void LateUpdate()
        {
            if (!_hasTarget || _pivotTarget == null) return;

            float dt = Time.unscaledDeltaTime;

            UpdateModeBlend(dt);
            UpdateMouseLook();

            _smoothedPivot = Vector3.SmoothDamp(_smoothedPivot, GetRawPivot(), ref _pivotVelocity,
                Mathf.Max(0.0001f, pivotFollowSmoothTime), Mathf.Infinity, dt);

            // 机位只解一次，遮挡淡出和弹簧臂用同一份（淡出要按「锚点 → 理想机位」这条真实线段来算）
            CameraPose pose = ResolvePose();
            UpdateObstacleFade(pose, dt);
            UpdateArm(dt, pose);
        }

        #region 状态机

        private void UpdateModeBlend(float dt)
        {
            if (_modeBlend >= 1f) return;

            _modeBlend = Mathf.Min(1f, _modeBlend + dt / Mathf.Max(0.0001f, GetBlendTime(_fromMode)));

            // 过渡彻底结束、且已经不在拼刀状态时，才放开对手引用（过渡中求解起始机位还要用它）
            if (_modeBlend >= 1f && _mode != CameraMode.Pin) _pinFocus = null;
        }

        private float GetBlendTime(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Pin: return pinBlendTime;
                case CameraMode.Ex: return exBlendTime;
                default: return pinBlendTime;
            }
        }

        // 混合「过渡来源状态」与「当前状态」的机位；两边都用实时数据求解，过渡中角色移动也不会脱节
        private CameraPose ResolvePose()
        {
            CameraPose target = ComputePose(_mode);
            if (_modeBlend >= 1f) return target;

            CameraPose from = ComputePose(_fromMode);
            float t = Mathf.SmoothStep(0f, 1f, _modeBlend);

            return new CameraPose
            {
                Pivot = Vector3.Lerp(from.Pivot, target.Pivot, t),
                Rotation = Quaternion.Slerp(from.Rotation, target.Rotation, t),
                ARMLength = Mathf.Lerp(from.ARMLength, target.ARMLength, t),
                FOV = Mathf.Lerp(from.FOV, target.FOV, t),
            };
        }

        private CameraPose ComputePose(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Pin: return ComputePinPose();
                case CameraMode.Ex: return ComputeExPose();
                default: return ComputeNormalPose();
            }
        }

        private CameraPose ComputeNormalPose()
        {
            return new CameraPose
            {
                Pivot = _smoothedPivot,
                Rotation = Quaternion.Euler(_pitch, _yaw, 0f),
                ARMLength = _targetArmLength,
                FOV = _baseFov,
            };
        }

        // 拼刀：锚点取两人中点，镜头按 pinYawOffset 绕到连线侧面 —— 构图只跟两人相对位置有关
        private CameraPose ComputePinPose()
        {
            Vector3 playerPos = _ownerRoot != null ? _ownerRoot.position : _smoothedPivot;
            Vector3 focusPos = _pinFocus != null ? _pinFocus.position : playerPos;

            // 基准朝向 = 玩家指向对手（水平），再绕 Y 偏 pinYawOffset 度绕到侧面
            Vector3 toFocus = focusPos - playerPos;
            toFocus.y = 0f;
            Quaternion baseRotation = toFocus.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(toFocus.normalized)
                : GetOwnerFlatRotation();

            return new CameraPose
            {
                Pivot = (playerPos + focusPos) * 0.5f + Vector3.up * pinHeight,
                Rotation = baseRotation * Quaternion.Euler(pinPitch, pinYawOffset, 0f),
                ARMLength = pinDistance,
                FOV = pinFov,
            };
        }

        // 大招：锚点取角色，镜头绕到角色侧前方；基准朝向进入时锁定，角色转身不会把镜头甩走
        private CameraPose ComputeExPose()
        {
            Vector3 playerPos = _ownerRoot != null ? _ownerRoot.position : _smoothedPivot;
            Quaternion baseRotation = exLockBaseYaw
                ? Quaternion.Euler(0f, _lockedBaseYaw, 0f)
                : GetOwnerFlatRotation();

            return new CameraPose
            {
                Pivot = playerPos + Vector3.up * exPivotHeight,
                Rotation = baseRotation * Quaternion.Euler(exPitch, exYawOffset, 0f),
                ARMLength = exDistance,
                FOV = exFov,
            };
        }

        #endregion

        #region 内部实现

        // 转视角 / 缩放。开面板会解锁光标并压入输入锁，两种情况下都不该再动镜头。
        private void UpdateMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || !InputManager.Instance.IsGameplayInputEnabled)
            {
                ResetMouseSmoothing();
                return;
            }

            float scroll = Input.GetAxis(MouseScrollAxis);
            if (Mathf.Abs(scroll) > 0.0001f)
                _targetArmLength = Mathf.Clamp(_targetArmLength - scroll * zoomSpeed, minArmLength, maxArmLength);

            // 特写期间机位由状态机接管，鼠标不参与
            if (_mode != CameraMode.Normal)
            {
                ResetMouseSmoothing();
                return;
            }

            Vector2 delta = ReadMouseDelta();
            _yaw += delta.x * lookSensitivity;
            // 俯仰：invertLookY 打开时「鼠标向下 = 相机向下」（pitch 减小 → 相机绕到角色下方）
            _pitch = Mathf.Clamp(_pitch + delta.y * lookSensitivity * (invertLookY ? 1f : -1f), pitchMin, pitchMax);
        }

        // 鼠标增量：仿 UE 的鼠标平滑 —— 正常移动走滑动平均，快速甩视角时直接用原始值
        private Vector2 ReadMouseDelta()
        {
            Vector2 raw = new Vector2(Input.GetAxisRaw(MouseXAxis), Input.GetAxisRaw(MouseYAxis));

            if (!enableMouseSmoothing || mouseSmoothingSamples <= 1)
            {
                _smoothedMouseDelta = raw;
                return raw;
            }

            float alpha = 1f / Mathf.Max(2, mouseSmoothingSamples);
            _smoothedMouseDelta = Vector2.Lerp(_smoothedMouseDelta, raw, alpha);

            bool quickFlick = raw.sqrMagnitude > mouseSmoothThreshold * mouseSmoothThreshold;
            return quickFlick ? raw : _smoothedMouseDelta;
        }

        private void ResetMouseSmoothing()
        {
            _smoothedMouseDelta = Vector2.zero;
        }

        // 正常视角的跟随点：角色模型 LookAt 位置 + 高度偏移
        private Vector3 GetRawPivot()
        {
            Vector3 basePos = _pivotTarget != null ? _pivotTarget.position : transform.position;
            return basePos + targetOffset;
        }

        private Quaternion GetOwnerFlatRotation()
        {
            Transform source = _facingSource != null ? _facingSource : _ownerRoot;
            Vector3 forward = source != null ? source.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized);
        }

        // 不考虑碰撞与滞后时的理想相机位置
        private Vector3 ComputeIdealLocation(CameraPose pose)
        {
            Vector3 socket = pose.Pivot + pose.Rotation * socketOffset;
            return socket + pose.Rotation * Vector3.back * pose.ARMLength;
        }

        // 每帧解算弹簧臂：滞后 → 碰撞推近 → 落位
        private void UpdateArm(float dt, CameraPose pose)
        {
            Quaternion rotation = pose.Rotation;
            Vector3 socket = pose.Pivot + rotation * socketOffset;
            Vector3 ideal = socket + rotation * Vector3.back * pose.ARMLength;

            // 只有正常视角才做位置滞后：特写是设计好的机位，拖尾反而糊
            bool useLag = enableCameraLocationLag && _armReady && _mode == CameraMode.Normal;
            if (useLag)
            {
                float lagSpeed = cameraLagSpeed;
                // 与理想位置拉得太开（传送 / 冲刺）时不再滞后，直接跟上
                if (cameraLagMaxDistance > 0f &&
                    (ideal - _laggedLocation).sqrMagnitude > cameraLagMaxDistance * cameraLagMaxDistance)
                {
                    lagSpeed = 0f;
                }

                _laggedLocation = InterpTo(_laggedLocation, ideal, dt, lagSpeed);
            }
            else
            {
                _laggedLocation = ideal;
            }

            _armReady = true;

            Vector3 offset = _laggedLocation - socket;
            if (offset.sqrMagnitude < 0.000001f) offset = rotation * Vector3.back;
            Vector3 direction = offset.normalized;
            float want = offset.magnitude;

            // 相机与跟随点之间被墙 / 地形挡住时把镜头推近，避免穿模
            float allowed = doCollisionTest ? ResolveObstructedDistance(socket, direction, want) : want;

            _camera.transform.position = socket + direction * allowed;

            if (enableCameraRotationLag && _mode == CameraMode.Normal)
            {
                _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, rotation,
                    Mathf.Clamp01(dt * cameraRotationLagSpeed));
            }
            else
            {
                _camera.transform.rotation = rotation;
            }

            _camera.fieldOfView = pose.FOV;

            ApplyShake();
        }

        private float ResolveObstructedDistance(Vector3 origin, Vector3 direction, float wantDistance)
        {
            if (wantDistance <= 0.0001f) return wantDistance;

            int count = Physics.SphereCastNonAlloc(origin, probeSize, direction, HitBuffer, wantDistance,
                ~0, QueryTriggerInteraction.Ignore);

            float nearest = wantDistance;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = HitBuffer[i].collider;
                if (hitCollider == null) continue;
                if (_ownerRoot != null && hitCollider.transform.IsChildOf(_ownerRoot)) continue;
                if (HitBuffer[i].distance < nearest) nearest = HitBuffer[i].distance;
            }

            if (nearest >= wantDistance) return wantDistance;
            return Mathf.Max(0.2f, nearest - collisionPadding);
        }

        private void ApplyShake()
        {
            if (_shakeTimer <= 0f) return;

            _shakeTimer -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.0001f, shakeDuration));
            float amp = _shakeAmp * shakeScale * fade;

            _camera.transform.position += Random.insideUnitSphere * amp;
            _camera.transform.rotation *= Quaternion.Euler(
                Random.Range(-1f, 1f) * amp * 6f, Random.Range(-1f, 1f) * amp * 6f, 0f);

            if (_shakeTimer <= 0f) _shakeAmp = 0f;
        }

        // UE 的 VInterpTo：按 dt * speed 的比例线性逼近目标，帧率无关
        private static Vector3 InterpTo(Vector3 current, Vector3 target, float dt, float speed)
        {
            if (speed <= 0f) return current;
            return Vector3.Lerp(current, target, Mathf.Clamp01(dt * speed));
        }

        #endregion

        // 一个机位：锚点 + 朝向 + 臂长 + FOV。状态机就是在这套参数上做插值。
        private struct CameraPose
        {
            public Vector3 Pivot;
            public Quaternion Rotation;
            public float ARMLength;
            public float FOV;
        }

        #region 遮挡淡出

        // 一条淡出记录：某个 Renderer 的原始材质 + 当前透明度
        private sealed class ObstacleFadeEntry
        {
            public Renderer Target;
            public Material[] Originals;
            public Color[] Colors;
            public Texture[] Textures;
            public MaterialPropertyBlock Block;
            public float Alpha = 1f;
            public float AppliedAlpha = -1f;
            public bool FadeMaterialApplied;
        }

        private readonly Dictionary<Renderer, ObstacleFadeEntry> _fadeEntries = new();
        private readonly HashSet<Renderer> _fadeHits = new();
        private readonly List<Renderer> _fadeExpired = new();
        private readonly RaycastHit[] _fadeHitBuffer = new RaycastHit[24];

        private Material _fadeMaterial;
        private bool _fadeShaderResolved;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const string FadeShaderName = "UnderWord/CameraObstacleFade";
        private const string FadeShaderResourcePath = "Shaders/CameraObstacleFade";

        // 每帧：先收集「锚点 → 理想机位」这条线段上的遮挡物，再按速度淡出 / 恢复。
        // 用「理想机位」而不是相机当前位置，这样镜头被碰撞推近时墙也照样淡出（否则贴着墙只会看到一堵满屏的墙）。
        private void UpdateObstacleFade(CameraPose pose, float dt)
        {
            _fadeHits.Clear();
            if (enableObstacleFade && GetFadeMaterial() != null) CollectObstacleFadeHits(pose);

            // 本帧没被挡的记录走向恢复；恢复完就还原材质并从表里移除
            foreach (KeyValuePair<Renderer, ObstacleFadeEntry> pair in _fadeEntries)
            {
                ObstacleFadeEntry entry = pair.Value;

                // 障碍物随时可能被销毁（特效回收、敌人死亡 / 对象池），销毁后必须直接丢弃记录，
                // 绝不能再碰它的 material —— 那就是 MissingReferenceException 的来源
                if (entry.Target == null)
                {
                    entry.FadeMaterialApplied = false;
                    _fadeExpired.Add(pair.Key);
                    continue;
                }

                if (_fadeHits.Contains(entry.Target)) continue;
                if (AdvanceFade(entry, 1f, dt)) _fadeExpired.Add(pair.Key);
            }

            for (int i = 0; i < _fadeExpired.Count; i++) _fadeEntries.Remove(_fadeExpired[i]);
            _fadeExpired.Clear();

            foreach (Renderer renderer in _fadeHits)
            {
                if (renderer == null) continue;

                if (!_fadeEntries.TryGetValue(renderer, out ObstacleFadeEntry entry))
                {
                    entry = CreateFadeEntry(renderer);
                    if (entry == null) continue;
                    _fadeEntries[renderer] = entry;
                }

                AdvanceFade(entry, obstacleFadeAlpha, dt);
            }
        }

        // 收集本帧的遮挡物
        private void CollectObstacleFadeHits(CameraPose pose)
        {
            Vector3 socket = pose.Pivot + pose.Rotation * socketOffset;
            Vector3 ideal = socket + pose.Rotation * Vector3.back * pose.ARMLength;

            Vector3 segment = ideal - socket;
            float distance = segment.magnitude;
            if (distance < 0.05f) return;
            Vector3 direction = segment / distance;

            int count = Physics.RaycastNonAlloc(socket, direction, _fadeHitBuffer, distance,
                obstacleFadeLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider collider = _fadeHitBuffer[i].collider;
                if (collider == null) continue;

                // 角色自己的碰撞体（CharacterController 也是 Collider）不算遮挡
                if (_ownerRoot != null && collider.transform.IsChildOf(_ownerRoot)) continue;

                Renderer renderer = ResolveFadeRenderer(collider);
                if (renderer == null || !CanFade(renderer)) continue;

                _fadeHits.Add(renderer);
            }

            if (debugDrawObstacleFade)
                Debug.DrawLine(socket, ideal, _fadeHits.Count > 0 ? Color.red : Color.green);
        }

        // 碰撞体常常挂在网格的子节点上，先找自己再往上找
        private static Renderer ResolveFadeRenderer(Collider collider)
        {
            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer == null) renderer = collider.GetComponentInParent<Renderer>();
            return renderer;
        }

        // 粒子 / 拖尾 / 线条这些特效不参与淡出：它们随时会被回收，淡出没有意义还会踩空引用
        private static bool CanFade(Renderer renderer)
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                return false;
            return true;
        }

        // 建记录时把原材质的外观（颜色 / 贴图）缓存下来，之后每帧只改 alpha，不再反复读材质
        private ObstacleFadeEntry CreateFadeEntry(Renderer renderer)
        {
            Material[] originals = renderer.sharedMaterials;
            int count = originals != null ? originals.Length : 0;
            if (count == 0) return null;

            ObstacleFadeEntry entry = new ObstacleFadeEntry
            {
                Target = renderer,
                Originals = originals,
                Block = new MaterialPropertyBlock(),
                Colors = new Color[count],
                Textures = new Texture[count],
            };

            for (int i = 0; i < count; i++)
            {
                entry.Colors[i] = Color.white;

                Material source = originals[i];
                if (source == null) continue; // 含「已被销毁」的情况，这里必须掉头

                // URP 用 _BaseColor / _BaseMap，内置管线用 _Color / _MainTex，两套都认
                entry.Colors[i] = source.HasProperty(BaseColorId) ? source.GetColor(BaseColorId) : source.GetColor(ColorId);
                if (source.HasProperty(BaseMapId)) entry.Textures[i] = source.GetTexture(BaseMapId);
                else if (source.HasProperty(MainTexId)) entry.Textures[i] = source.GetTexture(MainTexId);
            }

            return entry;
        }

        // 推进透明度；返回 true 表示已经完全恢复不透明（材质已还原，可以从表里移除）
        private bool AdvanceFade(ObstacleFadeEntry entry, float goal, float dt)
        {
            entry.Alpha = Mathf.MoveTowards(entry.Alpha, Mathf.Clamp01(goal),
                Mathf.Max(0.01f, obstacleFadeSpeed) * dt);

            if (entry.Alpha >= 0.999f)
            {
                RestoreFadeEntry(entry);
                return true;
            }

            ApplyFadeMaterial(entry, entry.Alpha);
            return false;
        }

        // 换上共享的淡出材质（整份材质只 new 一次），逐子材质用 MaterialPropertyBlock 写透明度。
        // 全程不碰 renderer.material —— 不会产生材质实例、不会把物体原本的 shader 改坏、不破 SRP Batcher 的静态批。
        private void ApplyFadeMaterial(ObstacleFadeEntry entry, float alpha)
        {
            Renderer renderer = entry.Target;
            if (renderer == null) return;

            if (!entry.FadeMaterialApplied)
            {
                Material[] mats = new Material[entry.Originals.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _fadeMaterial;
                renderer.sharedMaterials = mats;
                entry.FadeMaterialApplied = true;
                entry.AppliedAlpha = -1f;
            }

            if (Mathf.Abs(entry.AppliedAlpha - alpha) < 0.0005f) return;

            for (int i = 0; i < entry.Originals.Length; i++)
            {
                Color color = entry.Colors[i];
                color.a = alpha;

                Texture texture = entry.Textures[i] != null ? entry.Textures[i] : Texture2D.whiteTexture;

                entry.Block.Clear();
                entry.Block.SetTexture(BaseMapId, texture);
                entry.Block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(entry.Block, i);
            }

            entry.AppliedAlpha = alpha;
        }

        private void RestoreFadeEntry(ObstacleFadeEntry entry)
        {
            Renderer renderer = entry.Target;
            if (renderer != null && entry.FadeMaterialApplied)
            {
                renderer.sharedMaterials = entry.Originals;
                renderer.SetPropertyBlock(null);
            }

            entry.FadeMaterialApplied = false;
            entry.Alpha = 1f;
            entry.AppliedAlpha = -1f;
        }

        private void ClearObstacleFade()
        {
            if (_fadeEntries.Count == 0) return;

            foreach (KeyValuePair<Renderer, ObstacleFadeEntry> pair in _fadeEntries) RestoreFadeEntry(pair.Value);
            _fadeEntries.Clear();
            _fadeHits.Clear();
            _fadeExpired.Clear();
        }

        private Material GetFadeMaterial()
        {
            if (_fadeMaterial != null) return _fadeMaterial;
            if (_fadeShaderResolved) return null; // 找过一次没找到就不每帧再找

            _fadeShaderResolved = true;

            Shader shader = obstacleFadeShader != null ? obstacleFadeShader : Shader.Find(FadeShaderName);
            if (shader == null) shader = Resources.Load<Shader>(FadeShaderResourcePath);

            if (shader == null)
            {
                Debug.LogWarning($"[PlayerCameraRig] 找不到遮挡淡出 Shader「{FadeShaderName}」，已跳过遮挡淡出。" +
                                 "检查 Assets/Resources/Shaders/CameraObstacleFade.shader 是否还在。");
                return null;
            }

            _fadeMaterial = new Material(shader) { name = "CameraObstacleFade (Runtime)" };
            return _fadeMaterial;
        }

        #endregion
    }
}