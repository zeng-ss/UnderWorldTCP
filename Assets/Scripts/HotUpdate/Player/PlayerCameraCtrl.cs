using System.Collections.Generic;
using DG.Tweening;
using HotUpdate.Core;
using HotUpdate.Manager;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Player
{
    public class PlayerCameraCtrl : MonoBehaviour
    {
        private enum CameraState
        {
            Normal = 0,
            Pin = 1,
            Ult = 2
        }

        #region 相机引用

        [Header("相机")] [SerializeField] private Camera normalCamera;

        [SerializeField] private Camera pinCamera;

        [SerializeField] private Camera ultCamera;

        #endregion

        #region 可调参数 —— 正常视角

        [Header("① 正常 · 鼠标视角")] [SerializeField]
        private float lookSensitivity = 2.4f;

        [SerializeField] private bool invertLookY = true;

        [SerializeField] private bool enableMouseSmoothing = true;

        [Tooltip("滑动平均的采样帧数，越大越顺滑但越迟钝")] [SerializeField]
        private int mouseSmoothingSamples = 3;

        [Tooltip("单帧鼠标位移超过该值时视为快速甩视角，直接使用原始值不做平滑")] [SerializeField]
        private float mouseSmoothThreshold = 0.6f;

        [SerializeField] private float pitchMin = 10;
        [SerializeField] private float pitchMax = 30;
        [SerializeField] private float defaultPitch = 20;

        [Header("① 正常 · 弹簧臂 SpringArm")] [SerializeField]
        private Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);

        [Tooltip("肩位偏移：相机相对跟随点的横向/纵向拉偏，0 = 正后方")] [SerializeField]
        private Vector3 socketOffset = Vector3.zero;

        [SerializeField] private float targetArmLength = 6.5f;
        [SerializeField] private float minArmLength = 2.2f;
        [SerializeField] private float maxArmLength = 12f;
        [SerializeField] private float zoomSpeed = 2.2f;
        [SerializeField] private float pivotFollowSmoothTime = 0.03f;

        [Header("①  · 相机滞后 CameraLag")] [SerializeField]
        private bool enableCameraLocationLag = false;

        [Tooltip("相机位置逼近理想位置的速度")] [SerializeField]
        private float cameraLagSpeed = 14f;

        [Tooltip("滞后距离上限")] [SerializeField] private float cameraLagMaxDistance = 3.5f;

        [SerializeField] private bool enableCameraRotationLag;
        [SerializeField] private float cameraRotationLagSpeed = 18f;

        [Header("①  · 碰撞")] [SerializeField] private bool doCollisionTest = true;
        [Tooltip("探测球半径")] [SerializeField] private float probeSize = 0.3f;
        [SerializeField] private float collisionPadding = 0.1f;

        #endregion

        #region 可调参数 —— 大招过场

        [Header("② 大招过场")] [Tooltip("过场的 RenderTexture 宽度")] [SerializeField]
        private int ultTextureWidth = 1920;

        [Tooltip("黑边滑出动画播完才收相机")] [SerializeField]
        private float ultExitDelay = 0.4f;

        #endregion

        #region 可调参数 —— 遮挡淡出

        [Header("③ 遮挡淡出（相机与角色之间挡视线的物体变透明）")] [SerializeField]
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

        private Transform _pivotTarget; // 正常视角的跟随目标
        private Transform _facingSource; // 角色朝向来源

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
        private int _normalCullingMask;

        private CameraState _state = CameraState.Normal;
        private Vector3 _cutsceneBasePos; // 摆好的特写机位：每帧从这份基准重算，震屏才不会累积偏移
        private Quaternion _cutsceneBaseRot;

        private float _shakeTimer;
        private float _shakeAmp;

        #region 生命周期

        private void Awake()
        {
            if (normalCamera == null) normalCamera = Camera.main;
            if (normalCamera == null)
            {
                Debug.LogError("[PlayerCameraCtrl] 找不到正常视角相机（Inspector 没拖 + 场景里没有 MainCamera）。");
                enabled = false;
                return;
            }

            _baseFov = normalCamera.fieldOfView;
            _normalCullingMask = normalCamera.cullingMask;
            _targetArmLength = targetArmLength;

            // 特写相机要渲染在主相机之后才能盖住它（预制体里没设 depth 靠这里兜底）
            pinCamera.depth = normalCamera.depth + 1;
            pinCamera.gameObject.SetActive(false);
            ultCamera.enabled = false;
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

        public void Init(Transform playerModel)
        {
            if (normalCamera == null) return; // 场景里没有主相机

            Transform lookAt = playerModel.Find("LookAt");
            _pivotTarget = lookAt != null ? lookAt : playerModel;
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
            _hasTarget = true;

            CameraPose pose = ComputeNormalPose();
            _laggedLocation = ComputeIdealLocation(pose);
            UpdateArm(0f, pose);

            // 预热过场面板，第一次放大招才不会被异步加载拖半拍
            AppContext.UILoader.LoadAsync(PanelPathResolver.Resolve<UltPanel>(), null);
        }

        public Transform NormalCamera => normalCamera.transform;

        #region 外部指令

        public void Shake(float strength)
        {
            if (strength <= 0f) return;
            _shakeAmp = Mathf.Max(_shakeAmp, strength);
            _shakeTimer = shakeDuration;
        }

        public void EnterPin() => SwitchState(CameraState.Pin);

        public void ExitPin()
        {
            if (_state == CameraState.Pin) SwitchState(CameraState.Normal);
        }

        public void EnterUlt()
        {
            SwitchState(CameraState.Ult);
            PlayCutscene();
        }

        public void ExitUlt()
        {
            StopCutscene();
            if (_state == CameraState.Ult) SwitchState(CameraState.Normal);
        }

        private void SwitchState(CameraState state)
        {
            if (_state == state) return;
            _state = state;

            normalCamera.cullingMask = state == CameraState.Pin ? 0 : _normalCullingMask;
            pinCamera.gameObject.SetActive(state == CameraState.Pin);

            if (state != CameraState.Normal)
            {
                Camera cutscene = state == CameraState.Pin ? pinCamera : ultCamera;
                _cutsceneBasePos = cutscene.transform.localPosition;
                _cutsceneBaseRot = cutscene.transform.localRotation;
            }
        }

        #endregion

        #region 大招过场

        private RenderTexture _texture;
        private bool _cutscenePlaying;

        private void PlayCutscene()
        {
            if (_cutscenePlaying || ultCamera is null) return;
            _cutscenePlaying = true;

            // RT 高度跟屏幕比例走，全屏显示时才不会被拉伸
            int height = Mathf.Clamp(Mathf.RoundToInt(ultTextureWidth * (float)Screen.height / Screen.width), 480,
                1600);
            _texture = new RenderTexture(ultTextureWidth, height, 24);

            ultCamera.gameObject.SetActive(true);
            ultCamera.aspect = (float)ultTextureWidth / height;
            ultCamera.targetTexture = _texture;
            ultCamera.enabled = true;

            AppContext.Ui.OpenPanel<UltPanel>(panel => panel?.Play(_texture));
        }

        private void StopCutscene()
        {
            if (!_cutscenePlaying) return;
            _cutscenePlaying = false;

            AppContext.Ui.ClosePanel<UltPanel>();
            DOVirtual.DelayedCall(ultExitDelay, () =>
            {
                ultCamera.gameObject.SetActive(false);
                ultCamera.enabled = false;
                ultCamera.targetTexture = null;
                Destroy(_texture);
                _texture = null;
            });
        }

        #endregion

        private void LateUpdate()
        {
            if (!_hasTarget) return;
            float dt = Time.unscaledDeltaTime;

            if (_state == CameraState.Normal)
            {
                UpdateMouseLook();

                _smoothedPivot = Vector3.SmoothDamp(_smoothedPivot, GetRawPivot(), ref _pivotVelocity,
                    Mathf.Max(0.0001f, pivotFollowSmoothTime), Mathf.Infinity, dt);

                // 机位只解一次，遮挡淡出和弹簧臂用同一份（淡出要按「锚点 → 理想机位」这条真实线段来算）
                CameraPose pose = ComputeNormalPose();
                UpdateObstacleFade(pose, dt);
                UpdateArm(dt, pose);
            }
            else
            {
                Camera cutscene = _state == CameraState.Pin ? pinCamera : ultCamera;
                if (cutscene == null) return;

                // 摆好的机位：每帧先回到基准姿态再叠震屏，否则抖动会一直累积、镜头再也回不去
                cutscene.transform.SetLocalPositionAndRotation(_cutsceneBasePos, _cutsceneBaseRot);
                ApplyShake(cutscene, dt);
                return;
            }

            ApplyShake(normalCamera, dt);
        }

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

            Vector2 delta = ReadMouseDelta();
            _yaw += delta.x * lookSensitivity;
            // 俯仰：invertLookY 打开时「鼠标向下 = 相机向下」（pitch 减小 → 相机绕到角色下方）
            _pitch = Mathf.Clamp(_pitch + delta.y * lookSensitivity * (invertLookY ? 1f : -1f), pitchMin, pitchMax);
        }

        // 鼠标增量：正常移动走滑动平均，快速甩视角时直接用原始值
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

            if (enableCameraLocationLag && _armReady)
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

            normalCamera.transform.position = socket + direction * allowed;

            if (enableCameraRotationLag)
            {
                normalCamera.transform.rotation = Quaternion.Slerp(normalCamera.transform.rotation, rotation,
                    Mathf.Clamp01(dt * cameraRotationLagSpeed));
            }
            else
            {
                normalCamera.transform.rotation = rotation;
            }

            normalCamera.fieldOfView = pose.FOV;
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
                if (hitCollider.transform.IsChildOf(transform)) continue;
                if (HitBuffer[i].distance < nearest) nearest = HitBuffer[i].distance;
            }

            if (nearest >= wantDistance) return wantDistance;
            return Mathf.Max(0.2f, nearest - collisionPadding);
        }

        private void ApplyShake(Camera cam, float dt)
        {
            if (_shakeTimer <= 0f) return;

            _shakeTimer -= dt;
            float fade = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.0001f, shakeDuration));
            float amp = _shakeAmp * shakeScale * fade;

            cam.transform.position += Random.insideUnitSphere * amp;
            cam.transform.rotation *= Quaternion.Euler(
                Random.Range(-1f, 1f) * amp * 6f, Random.Range(-1f, 1f) * amp * 6f, 0f);

            if (_shakeTimer <= 0f) _shakeAmp = 0f;
        }

        // UE 的 VInterpTo：按 dt * speed 的比例线性逼近目标，帧率无关
        private static Vector3 InterpTo(Vector3 current, Vector3 target, float dt, float speed)
        {
            if (speed <= 0f) return current;
            return Vector3.Lerp(current, target, Mathf.Clamp01(dt * speed));
        }

        // 正常视角的机位：锚点 + 朝向 + 臂长 + FOV
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

        #endregion

        // 一个机位：锚点 + 朝向 + 臂长 + FOV
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
                if (collider.transform.IsChildOf(transform)) continue;

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
                entry.Colors[i] = source.HasProperty(BaseColorId)
                    ? source.GetColor(BaseColorId)
                    : source.GetColor(ColorId);
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
                Debug.LogWarning($"[PlayerCameraCtrl] 找不到遮挡淡出 Shader「{FadeShaderName}」，已跳过遮挡淡出。" +
                                 "检查 Assets/Resources/Shaders/CameraObstacleFade.shader 是否还在。");
                return null;
            }

            _fadeMaterial = new Material(shader) { name = "CameraObstacleFade (Runtime)" };
            return _fadeMaterial;
        }

        #endregion
    }
}