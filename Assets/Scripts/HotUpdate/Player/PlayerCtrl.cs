using System;
using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerCtrl : MonoBehaviour, IState_MachineOwner, ISkillOwner, IHurt
{
    #region 参数

    private PlayerProPanel _playerProPanel;
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Transform cameraTransform;
    [HideInInspector] public State_Machine StateMachine;
    public PlayerModel playerModel;
    private AudioSource _audioSource;
    [HideInInspector] public CinemachineImpulseSource impulseSource;
    private ChromaticAberration _chromaticAberration;
    [HideInInspector] public Vignette vignette;
    private const float Gravity = -5f;
    private Vector3 _velocity;
    [HideInInspector] public bool isLock;
    public float rotationSpeed;
    [HideInInspector] public bool hasGravity;
    [HideInInspector] public bool isOnGround;
    [HideInInspector] public Vector3 currentMoveDir;
    public PlayerStateType currentState;
    public PlayerStateType lastState;
    public DamageNumber damageNumber;
    [HideInInspector] public EnemyCtrl enemy;
    [HideInInspector] public CinemachineVirtualCamera virtualCameraEx;
    private CinemachineVirtualCamera _virtualCameraPin;
    private CinemachineFreeLook _cinemachineFreeLook;

    #endregion

    #region 属性

    public float health = 200f;
    public float maxHealth = 200f;
    private PlayerData _playerData = new();
    public bool IsLocalPlayer { get; set; } = true;

    #endregion

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        characterController = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();
        playerModel.Init(this);
        StateMachine = new State_Machine();
        StateMachine.Init(this);
        if (skillConfigList is { Count: > 0 })
        {
            curSkillConfig = skillConfigList[0];
        }
    }

    private void Start()
    {
        if (!IsLocalPlayer) return;
        // 优先使用从服务器加载的角色数据
        MainRoleInfo info = GameManager.Instance.mainRoleInfo;
        if (info != null)
        {
            _playerData = new PlayerData
            {
                id = (ulong)info.BaseInfo.RoleId,
            };
        }
        else
        {
            health = _playerData.maxHealthValue;
            maxHealth = _playerData.maxHealthValue;
        }

        Init();
        characterController.enabled = true;
        ChangeState(PlayerStateType.Idle);
        _isInit = true;

        // 启动位置同步（position 走 root transform，rotation 走 model transform）
        ProtoHandler.Instance.StartPositionSync(transform, GameManager.Instance.roleId, playerModel.transform);
    }

    #region 初始化方法

    private void Init()
    {
        cameraTransform = Camera.main?.transform;
        characterController.enabled = true;
        hasGravity = true;
        _virtualCameraPin = GameObject.Find("VirtualCamera_Pin")?.GetComponent<CinemachineVirtualCamera>();
        virtualCameraEx = GameObject.Find("VirtualCamera_Ex")?.GetComponent<CinemachineVirtualCamera>();
        _cinemachineFreeLook = FindObjectOfType<CinemachineFreeLook>();
        Transform lookAtTarget = playerModel.transform.Find("LookAt");
        if (lookAtTarget)
        {
            _cinemachineFreeLook.Follow = lookAtTarget;
            _cinemachineFreeLook.LookAt = lookAtTarget;
            if (_virtualCameraPin)
            {
                _virtualCameraPin.Follow = transform;
                _virtualCameraPin.LookAt = transform;
                _virtualCameraPin.gameObject.SetActive(false);
            }

            if (virtualCameraEx)
            {
                virtualCameraEx.Follow = lookAtTarget;
                virtualCameraEx.LookAt = lookAtTarget;
                virtualCameraEx.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("找不到 lookAt 子物体，请检查角色层级");
        }

        GameObject.Find("Volume")?.GetComponent<Volume>()?.profile.TryGet(out _chromaticAberration);
        GameObject.Find("Volume")?.GetComponent<Volume>()?.profile.TryGet(out vignette);
    }

    #endregion

    private bool _isInit;

    private void Update()
    {
        if (!IsLocalPlayer || !_isInit) return;
        if (isLock) return;

        #region 拼刀

        if (Input.GetKeyDown(KeyCode.E) && !_isPining)
        {
            enemy = FindAnyObjectByType<EnemyCtrl>();
            if (enemy && enemy.isStartPin && !enemy.isPinFinished)
            {
                TeleportToEnemy();
                if (enemy.isStartPinTip) enemy.isStartPinTip.gameObject.SetActive(false);
                _isPining = true;
                DOVirtual.DelayedCall(1f, () =>
                {
                    _isPining = false;
                    _virtualCameraPin.gameObject.SetActive(false);
                });
                enemy.OnStiff();
            }
        }

        #endregion

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            health = Mathf.Clamp(health + 50, 50, maxHealth);
        }

        chatPanel ??= FindObjectOfType<ChatPanel>();
        if (!characterController.enabled && currentState != PlayerStateType.Dead) characterController.enabled = true;

        #region 重力

        if (!hasGravity || !characterController.enabled) return;
        characterController.Move(_velocity * Time.deltaTime);
        isOnGround = characterController.isGrounded;
        if (isOnGround)
        {
            _velocity.y = -2f;
        }
        else
        {
            _velocity.y += Gravity * Time.deltaTime;
        }

        #endregion

        HandleSkillSwitch();
        HandleEvadeSwitch();
    }

    #region 闪避相关

    private float _lastShiftPressTime;
    private const float DoubleTapInterval = 0.3f;
    private bool _isShiftDoubleTap;
    [HideInInspector] public ChatPanel chatPanel;

    private void HandleEvadeSwitch()
    {
        if (chatPanel && chatPanel.chatInput.isFocused) return;
        if (!Input.GetKeyDown(KeyCode.LeftShift)) return;
        _isShiftDoubleTap = Time.time - _lastShiftPressTime < DoubleTapInterval;

        ChangeState(PlayerStateType.Evade);
        PlayerEvadeState state = (PlayerEvadeState)StateMachine.CurrentState;
        state.SetEvade(_isShiftDoubleTap);
        _lastShiftPressTime = Time.time;
    }

    #endregion

    #region 动画和声音相关

    public void ChangeState(PlayerStateType stateType, bool isResfeshState = false)
    {
        if (isLock) return;
        if (currentState != stateType)
        {
            lastState = currentState;
        }

        currentState = stateType;
        switch (stateType)
        {
            case PlayerStateType.Idle: StateMachine.ChangeState<PlayerIdleState>(isResfeshState); break;
            case PlayerStateType.Move: StateMachine.ChangeState<PlayerMoveState>(isResfeshState); break;
            case PlayerStateType.Attack: StateMachine.ChangeState<PlayerAttackState>(isResfeshState); break;
            case PlayerStateType.Evade: StateMachine.ChangeState<PlayerEvadeState>(isResfeshState); break;
            case PlayerStateType.Dead: StateMachine.ChangeState<PlayerDeadState>(isResfeshState); break;
            case PlayerStateType.Hurt: StateMachine.ChangeState<PlayerHurtState>(isResfeshState); break;
            case PlayerStateType.EX: StateMachine.ChangeState<EXAttackState>(isResfeshState); break;
            default: throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
        }
    }

    public void PlayAnimation(string animationName, float fixedTransitionTime = 0.1f)
    {
        playerModel.Animator.CrossFadeInFixedTime(animationName, fixedTransitionTime, 0, 0f);
        ProtoHandler.Instance.RequestSyncAni(GameManager.Instance.roleId, animationName, _curSkillIndex,
            ret => { RemotePlayerManager.Instance.OnSyncAni(ret); });
    }

    private void PlayerAudio(AudioClip audioClip)
    {
        _audioSource.PlayOneShot(audioClip);
    }

    #endregion

    #region 技能连招配置切换

    private void HandleSkillSwitch()
    {
        if (!IsLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.Alpha2) && curSkillConfig != skillConfigList[1] && skillConfigList.Count > 1)
        {
            UpdateSkillConfig(1);
        }
        else if ((Input.GetKeyDown(KeyCode.Alpha1) ||
                  (Input.GetKeyDown(KeyCode.Mouse0) && curSkillConfig == skillConfigList[2]))
                 && curSkillConfig != skillConfigList[0])
        {
            if (skillConfigList.Count > 0) UpdateSkillConfig(0);
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1) && curSkillConfig != skillConfigList[2] && skillConfigList.Count > 2)
        {
            UpdateSkillConfig(2);
        }
        else if (Input.GetKeyDown(KeyCode.R) && curSkillConfig != skillConfigList[3])
        {
            UpdateSkillConfig(3);
            ChangeState(PlayerStateType.EX);
        }
    }

    public void UpdateSkillConfig(int skillIndex, bool isPin = false)
    {
        if (skillIndex < 0 || skillIndex >= skillConfigList.Count)
        {
            Debug.LogWarning($"切换连招索引越界：{skillIndex}，默认切回第一套");
            skillIndex = 0;
        }

        _curSkillIndex = skillIndex;
        CurAttackIndex = isPin ? 1 : 0;
        CurVFXIndex = isPin ? 1 : 0;
        curSkillConfig = skillConfigList[skillIndex];
    }

    #endregion

    #region 拼刀瞬移

    private bool _isPining;

    private void TeleportToEnemy()
    {
        if (!enemy) return;
        Vector3 dirToPlayer = (transform.position - enemy.transform.position).normalized;
        dirToPlayer.y = 0;
        Vector3 teleportPos = enemy.transform.position + dirToPlayer * 1f;
        teleportPos.y = transform.position.y;
        characterController.enabled = false;
        transform.position = teleportPos;
        characterController.enabled = true;
        _virtualCameraPin.gameObject.SetActive(true);
        transform.LookAt(enemy.transform);
        UpdateSkillConfig(1, true);
        ChangeState(PlayerStateType.Attack, true);
    }

    #endregion

    #region 技能相关

    private int _curAttackIndex;
    private int _curVFXIndex;
    private int _curSkillIndex;

    public int CurVFXIndex
    {
        get => _curVFXIndex;
        private set
        {
            if (CurAttackIndex == -1 || curSkillConfig == null || curSkillConfig.skillConfigs.Count <= CurAttackIndex)
            {
                _curVFXIndex = 0;
                return;
            }

            _curVFXIndex = value >= curSkillConfig.skillConfigs[CurAttackIndex].VFXDataList.Count ? 0 : value;
        }
    }

    public int CurAttackIndex
    {
        get => _curAttackIndex;
        set => _curAttackIndex = value >= curSkillConfig.skillConfigs.Count ? 0 : value;
    }

    [HideInInspector] public SkillConfig curSkillConfig;
    public List<SkillConfig> skillConfigList;
    public bool CanSwitchSkill { get; set; }

    public void StartSkill(AttackData attackData)
    {
        CurVFXIndex = 0;
        CanSwitchSkill = false;
        PlayAnimation(attackData.attackAnimationName);
        PlayerAudio(attackData.VFXDataList[CurVFXIndex].VFXClip);
    }

    public void StartSkillHit(int weaponIndex)
    {
        if (CurAttackIndex == -1) CurAttackIndex = 0;
        if (CurVFXIndex < 0 || CurVFXIndex >= curSkillConfig.skillConfigs[CurAttackIndex].VFXDataList.Count)
            CurVFXIndex = 0;
        StartCoroutine(DoSpawnVFX(curSkillConfig.skillConfigs[CurAttackIndex].VFXDataList[CurVFXIndex]));

        if (IsLocalPlayer)
        {
            ProtoHandler.Instance.RequestSyncVfx(new PlayerVfxNtf
            {
                RoleId = (int)_playerData.id,
                SkillConfigIndex = _curSkillIndex,
                AttackIndex = CurAttackIndex,
                VfxIndex = _curVFXIndex
            });
        }
    }

    private IEnumerator DoSpawnVFX(VFXData vfxData)
    {
        yield return new WaitForSeconds(vfxData.spawnTime);
        Vector3 worldPos = playerModel.transform.TransformPoint(vfxData.spawnPos);
        Quaternion worldRot = playerModel.transform.rotation * Quaternion.Euler(vfxData.spawnRot);
        Vector3 spawnScale = vfxData.spawnScale;
        var vfxObj = Instantiate(vfxData.prefab);
        vfxObj.GetComponent<ParticleCtrl>()?.Init(this);
        vfxObj.transform.position = worldPos;
        vfxObj.transform.rotation = worldRot;
        vfxObj.transform.localScale += spawnScale;
        Destroy(vfxObj, vfxObj.TryGetComponent<ParticleSystem>(out var ps) ? ps.main.duration : 2f);
    }

    public void SpawnRemoteVfx(int skillConfigIndex, int attackIndex, int vfxIndex)
    {
        if (skillConfigIndex < 0 || skillConfigIndex >= skillConfigList.Count) return;
        var config = skillConfigList[skillConfigIndex];
        if (attackIndex < 0 || attackIndex >= config.skillConfigs.Count) return;
        var vfxList = config.skillConfigs[attackIndex].VFXDataList;
        if (vfxIndex < 0 || vfxIndex >= vfxList.Count) return;
        StartCoroutine(DoSpawnVFX(vfxList[vfxIndex]));
    }

    public void StopSkillHit(int weaponIndex)
    {
        CurVFXIndex++;
    }

    public void SkillCanSwitch()
    {
        CanSwitchSkill = true;
    }

    #endregion

    #region 攻击同步

    public void OnHit(IHurt hurt, Vector3 hurtPos)
    {
        if (!IsLocalPlayer) return;
        PlayLocalHitEffects(hurtPos);
        hurt.OnHurt(curSkillConfig.skillConfigs[CurAttackIndex].HitData, this);

        // 发送攻击请求到服务端（联机时服务端权威验伤）
        EnemyCtrl enemy = ((Component)hurt).GetComponent<EnemyCtrl>();
        if (enemy != null && enemy.serverInstanceId > 0)
        {
            float baseDamage = currentState == PlayerStateType.EX
                ? _playerData.exAttackValue
                : _playerData.attackValue;
            Debug.Log("发送！");
            ProtoHandler.Instance.RequestPlayerAttack(
                GameManager.Instance.roleId,
                enemy.serverInstanceId,
                baseDamage,
                _playerData.baoJiValue,
                currentState == PlayerStateType.EX,
                enemy.OnServerAttackResult
            );
        }
    }

    private void PlayLocalHitEffects(Vector3 hurtPos)
    {
        if (CurAttackIndex == -1) CurAttackIndex = 0;
        HitData hitData = curSkillConfig.skillConfigs[CurAttackIndex].HitData;
        GameObject obj = Instantiate(hitData.hitPrefabs[CurVFXIndex]);
        obj.transform.position = hurtPos;
        Destroy(obj, obj.GetComponent<ParticleSystem>().main.duration);
        impulseSource.GenerateImpulse(hitData.screenImpulseValue);
        if (_chromaticAberration != null && vignette != null)
        {
            DOTween.To(() => _chromaticAberration.intensity.value, x => _chromaticAberration.intensity.value = x,
                    hitData.chromaticAberrationValue, 0.2f)
                .OnComplete(() => { _chromaticAberration.intensity.value = 0; });
        }

        SoundManager.Instance.PlaySound(hitData.hitClip, hurtPos);
    }

    #endregion

    #region 受伤相关

    public void OnHurt(HitData hitData, ISkillOwner hurtSource)
    {
        health -= Math.Max(0, hitData.damageValue - (float)Math.Round(_playerData.defenseValue / 10, 2));
        if (health <= 0)
        {
            OnDead();
            return;
        }

        OnHurtLocal(hitData.vignetteValue, hitData.screenImpulseValue, hitData.damageValue);
    }

    private void OnHurtLocal(float vignetteValue, float screenImpulseValue, float damageValue)
    {
        ChangeState(PlayerStateType.Hurt, true);
        damageNumber.Spawn(transform.position, damageValue);
        PlayPlayerHurtLocalFeedback(vignetteValue, screenImpulseValue);
    }

    private Tweener _vignetteTweener;

    private void PlayPlayerHurtLocalFeedback(float vignetteValue, float screenImpulseValue)
    {
        if (!IsLocalPlayer) return;
        impulseSource.GenerateImpulse(screenImpulseValue);
        if (vignette != null)
        {
            if (_vignetteTweener != null && _vignetteTweener.IsActive())
            {
                _vignetteTweener.Kill();
            }

            _vignetteTweener = DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x,
                    vignetteValue, 0.2f)
                .OnComplete(() =>
                {
                    DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, 0, 0.3f);
                });
        }
    }

    private void OnDead()
    {
        ChangeState(PlayerStateType.Dead);
        tag = "Untagged";
    }

    #endregion

    #region 血量血条

    public float GetHealth() => health;
    public float GetMaxHealth() => maxHealth;

    #endregion

    public PlayerData CalculatePlayerData(List<DriverDiskDataRuntime> depotsData)
    {
        float baoJi = GameManager.Instance.playerBaseData.baoJiValue;
        float attack = GameManager.Instance.playerBaseData.attackValue;
        float defense = GameManager.Instance.playerBaseData.defenseValue;
        float healthValue = GameManager.Instance.playerBaseData.maxHealthValue;
        float exAttackValue = GameManager.Instance.playerBaseData.exAttackValue;
        foreach (var depot in depotsData)
        {
            baoJi = (float)Math.Round(baoJi * (1 + depot.DepotDriverDiskValue.baoJiPercent / 100), 2);
            attack = (float)Math.Round(attack * (1 + depot.DepotDriverDiskValue.attackPercent / 100), 2);
            healthValue = (float)Math.Round(healthValue * (1 + depot.DepotDriverDiskValue.healthPercent / 100), 2);
            defense = (float)Math.Round(defense * (1 + depot.DepotDriverDiskValue.defensePercent / 100), 2);
            exAttackValue = (float)Math.Round(exAttackValue * (1 + depot.DepotDriverDiskValue.attackPercent / 100), 2);
            switch (depot.DepotDriverDiskValue.driverDiskType)
            {
                case DriverDiskType.Attack: attack += depot.DepotDriverDiskValue.baseValue; break;
                case DriverDiskType.BaoJi: baoJi += depot.DepotDriverDiskValue.baseValue; break;
                case DriverDiskType.Defense: defense += depot.DepotDriverDiskValue.baseValue; break;
                case DriverDiskType.Health: healthValue += depot.DepotDriverDiskValue.baseValue; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        return new PlayerData
        {
            id = 0,
            maxHealthValue = healthValue,
            attackValue = attack,
            defenseValue = defense,
            baoJiValue = baoJi,
            exAttackValue = exAttackValue
        };
    }

    public void UpdatePlayerData(PlayerData newPlayerData)
    {
        _playerData = newPlayerData;
        maxHealth = newPlayerData.maxHealthValue;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener(GameEvent.游戏开始, Init);
    }
}