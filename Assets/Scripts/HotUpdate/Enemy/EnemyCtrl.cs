using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DamageNumbersPro;

public class EnemyCtrl : MonoBehaviour, IHurt, ISkillOwner, IState_MachineOwner
{
    /// <summary>
    /// 按服务端实例ID查找敌人（联机时其他人攻击广播更新血量用）
    /// </summary>
    public static Dictionary<int, EnemyCtrl> Instances = new();

    public bool IsLocalEnemy => GameManager.Instance.roleId == roleId;

    public int roleId;

    #region 数据

    public HitData hitData;
    [HideInInspector] public float disToPlayer;
    public EnemyModel enemyModel;
    private AudioSource audioSource;
    private State_Machine stateMachine;
    private CapsuleCollider capsuleCollider;
    [HideInInspector] public CharacterController characterController;
    private PlayerCtrl _playerRef;

    public PlayerCtrl PlayerRef
    {
        get
        {
            if (_playerRef == null || _playerRef.currentState == PlayerStateType.Dead)
                _playerRef = FindNearestPlayer();
            return _playerRef;
        }
        set => _playerRef = value;
    }

    private float gravity = -5f;
    private Vector3 velocity;
    [HideInInspector] public bool hasGravity;
    [HideInInspector] public bool isOnGround;
    [HideInInspector] public EnemyStateType currentState;
    [HideInInspector] public EnemyStateType lastState;

    public bool IsServer { get; set; } = false;
    public float networkHealth = 100f;
    public float maxHealthValue = 20000f;
    [HideInInspector] public bool isCanPlayHurtAni = true;
    [HideInInspector] public bool isStartLock;
    public ulong requestSpawnId;
    public bool isStartPin;
    public bool isPinFinished;

    public GameObject healthBar;
    public Image fillImage;
    public TMP_Text healthText;
    public GameObject isStartPinTip;

    // 服务端敌人实例ID
    [HideInInspector] public int serverInstanceId;

    #endregion

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        characterController = GetComponent<CharacterController>();
        enemyModel.Init(this);
        stateMachine = new State_Machine();
        stateMachine.Init(this);
        ChangeState(EnemyStateType.Idle);

        isStartPin = false;
        maxHealthValue = 200f;
        networkHealth = maxHealthValue;
        isStartLock = false;
        isCanPlayHurtAni = true;
        capsuleCollider.enabled = false;
        _playerRef = FindNearestPlayer();
        hasGravity = true;
        isOnGround = characterController.isGrounded;

        fillImage.fillAmount = networkHealth / maxHealthValue;
        healthText.text = $"{networkHealth}/{maxHealthValue}";

        ProtoHandler.Instance.StartEPositionSync(transform, GameManager.Instance.roleId, enemyModel.transform);
    }

    private void Update()
    {
        if (!IsLocalEnemy) return;
        KeepLookPlayer(healthBar);
        if (isStartPinTip) KeepLookPlayer(isStartPinTip);
        _playerRef = FindNearestPlayer();
        if (_playerRef)
        {
            disToPlayer = Vector3.Distance(transform.position, _playerRef.transform.position);
        }

        #region 重力

        if (!hasGravity || !characterController.enabled) return;
        characterController.Move(velocity * Time.deltaTime);
        isOnGround = characterController.isGrounded;
        if (isOnGround)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        #endregion

        #region 锁定

        if (!_playerRef) return;
        if (currentState == EnemyStateType.Attack && disToPlayer >= 2f && isStartLock)
        {
            KeepLockDistance();
        }

        #endregion

        float fillAmount = Mathf.Clamp01(networkHealth / maxHealthValue);
        fillImage.fillAmount = fillAmount;
        healthText.text = $"{networkHealth}/{maxHealthValue}";
    }

    private void KeepLookPlayer(GameObject obj)
    {
        if (!obj || !Camera.main) return;
        obj.transform.LookAt(Camera.main.transform.position);
        obj.transform.Rotate(0, 180, 0);
    }

    #region 动画和声音

    public void ChangeState(EnemyStateType stateType, bool isResfeshState = false)
    {
        if (currentState != stateType)
        {
            lastState = currentState;
        }

        currentState = stateType;
        switch (stateType)
        {
            case EnemyStateType.Idle: stateMachine.ChangeState<EnemyIdleState>(isResfeshState); break;
            case EnemyStateType.Attack: stateMachine.ChangeState<EnemyAttackState>(isResfeshState); break;
            case EnemyStateType.Dead: stateMachine.ChangeState<EnemyDeadState>(isResfeshState); break;
            case EnemyStateType.Hurt: stateMachine.ChangeState<EnemyHurtState>(isResfeshState); break;
            default: throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
        }
    }

    public void PlayAnimation(string animationName, float fixedTransitionTime = 0.1f)
    {
        enemyModel.Animator.CrossFadeInFixedTime(animationName, fixedTransitionTime, 0, 0f);
        ProtoHandler.Instance.RequestSyncEnemyAni(GameManager.Instance.roleId, animationName,
            ret => { RemotePlayerManager.Instance.OnSyncAni(ret); });
    }

    private void EnemyAudio(AudioClip audioClip) => audioSource.PlayOneShot(audioClip);
    #endregion

    #region 玩家相关

    public void FaceToPlayer()
    {
        if (!_playerRef || currentState == EnemyStateType.Idle) return;
        Vector3 toPlayer = _playerRef.transform.position - transform.position;
        if (toPlayer == Vector3.zero) return;
        toPlayer.y = 0;
        enemyModel.transform.rotation = Quaternion.Slerp(enemyModel.transform.rotation,
            Quaternion.LookRotation(toPlayer), Time.deltaTime * 5f);
    }

    private PlayerCtrl FindNearestPlayer()
    {
        PlayerCtrl[] allPlayers = FindObjectsOfType<PlayerCtrl>();
        if (allPlayers.Length == 0) return null;
        PlayerCtrl nearest = null;
        float minDist = Mathf.Infinity;
        foreach (PlayerCtrl p in allPlayers)
        {
            if (p.currentState == PlayerStateType.Dead) continue;
            float dis = Vector3.Distance(transform.position, p.transform.position);
            if (dis < minDist)
            {
                minDist = dis;
                nearest = p;
            }
        }

        return nearest;
    }

    [SerializeField] private float lockDistance = 2f;
    [SerializeField] private float minSafeDistance = 2f;
    [SerializeField] private float maxSafeDistance = 2f;
    [SerializeField] private float moveSpeed = 5f;

    private void KeepLockDistance()
    {
        if (!_playerRef) return;
        Vector3 enemyPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(_playerRef.transform.position.x, 0, _playerRef.transform.position.z);
        Vector3 toPlayer = playerPos - enemyPos;
        float currentDistance = toPlayer.magnitude;

        if (currentDistance >= minSafeDistance && currentDistance <= maxSafeDistance)
        {
            FaceToPlayer();
            return;
        }

        if (currentDistance < minSafeDistance)
        {
            FaceToPlayer();
            Vector3 backDir = -toPlayer.normalized;
            backDir.y = 0;
            characterController.Move(backDir * (moveSpeed * Time.deltaTime));
            return;
        }

        if (currentDistance > maxSafeDistance)
        {
            FaceToPlayer();
            Vector3 targetPos = playerPos - toPlayer.normalized * lockDistance;
            targetPos.y = transform.position.y;
            Vector3 moveDir = targetPos - transform.position;
            moveDir.y = 0;
            moveDir = moveDir.normalized;
            characterController.Move(moveDir * (moveSpeed * Time.deltaTime));
        }
    }

    #endregion

    #region 技能

    public void StartSkillHit(int weaponIndex)
    {
    }

    public void StopSkillHit(int weaponIndex)
    {
    }

    public void SkillCanSwitch()
    {
    }

    #endregion

    #region 攻击

    public void OnHit(IHurt hurt, Vector3 hurtPos)
    {
        if (hitData == null) return;
        GameObject obj = Instantiate(hitData.hitPrefabs[0]);
        obj.transform.position = hurtPos;
        Destroy(obj, obj.GetComponent<ParticleSystem>().main.duration);
        SoundManager.Instance.PlaySound(hitData.hitClip, hurtPos);
        hurt.OnHurt(hitData, this);
    }

    #endregion

    #region 受伤

    public DamageNumber damageNumber;

    public void OnHurt(HitData hitData, ISkillOwner hurtSource)
    {
        ((Component)hurtSource).GetComponent<PlayerCtrl>();
    }

    /// <summary>
    /// 敌人接收服务端攻击结果，同步状态给所有客户端（联机时服务端权威）
    /// </summary>
    public void OnServerAttackResult(PlayerAttackRet ret)
    {
        if (ret.IsDead)
        {
            networkHealth = 0;
            OnEnemyDead();
            return;
        }

        networkHealth = ret.EnemyCurrHp;

        // 显示伤害数字和受伤动画
        Vector3 hurtPos = transform.position + Vector3.up * 1.5f;
        if (isCanPlayHurtAni)
        {
            ChangeState(EnemyStateType.Hurt, true);
            damageNumber.SetColor(ret.IsBaoji ? Color.red : Color.white);
            damageNumber.Spawn(hurtPos, ret.DamageDealt);
        }
    }

    private void OnEnemyDead()
    {
        Instances.Remove(serverInstanceId);
        TaskManager.Instance.UpdateTaskProgress(TaskType.击败第一个敌人);
        capsuleCollider.enabled = false;
        tag = "Untagged";
        ChangeState(EnemyStateType.Dead);
        DOVirtual.DelayedCall(5, () => { Destroy(gameObject); });
    }

    private void OnDestroy()
    {
        Instances.Remove(serverInstanceId);
    }

    #endregion

    #region 僵硬

    public void OnStiff()
    {
        if (isPinFinished) return;
        isStartPin = false;
        isPinFinished = true;
        isStartLock = false;
        enemyModel.Animator.speed = 0;
        DOVirtual.DelayedCall(0.5f, () =>
        {
            enemyModel.Animator.speed = 1;
            ChangeState(EnemyStateType.Idle);
        });
    }

    #endregion
}