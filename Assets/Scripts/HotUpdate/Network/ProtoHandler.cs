using System;
using System.Collections.Generic;
using Google.Protobuf;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Network
{
    /// <summary>
    /// 协议处理层：统一管理请求发送、响应处理、事件广播。
    /// </summary>
    public class ProtoHandler
    {
        #region 位置同步状态 => 玩家

        private Transform _syncTarget;
        private Transform _syncRotationTarget;
        private float _syncTimer;
        private string _syncNickname;
        private int _syncRoleId;

        #endregion

        #region 事件

        public event Action<PositionSyncNtf> OnPositionSyncReceived;
        public event Action<PlayerEnterSceneNtf> OnPlayerEnterScene;
        public event Action<PlayerLeaveSceneNtf> OnPlayerLeaveScene;
        public event Action<PlayerAttackRet> OnPlayerAttackBroadcast;
        public event Action<SyncAniRet> OnSyncAniReceived;
        public event Action<PlayerVfxNtf> OnPlayerVfxReceived;
        public event Action<EnemyPositionSyncRet> OnEnemyPositionSyncReceived;
        public event Action<EnemySyncAniRet> OnEnemySyncAniReceived;

        // 玩家受击广播（敌人攻击玩家，服务端结算）。场景内所有人都会收到，本地玩家据此更新血量。
        public event Action<PlayerHurtNtf> OnPlayerHurtReceived;
        public event Action<FriendMsgPushNtf> OnServerPushMessageReceived;

        #endregion

        #region 回调存储

        private Action<RegistRet> _registCallback;
        private Action<loginRet> _loginCallback;
        private Action<GetServerListRet> _serverListCallback;
        private Action<loginGameServerRet> _loginGameServerCallback;
        private Action<CreateRoleRet> _createRoleCallback;
        private Action<StartGameRet> _startGameCallback;
        private Action<SaveRoleRet> _saveRoleCallback;
        private Action<ChangeSceneRet> _changeSceneCallback;
        private Action<SpawnEnemyRet> _spawnEnemyCallback;
        private readonly Dictionary<int, Action<PlayerAttackRet>> _playerAttackCallbacks = new();
        private int _nextAttackSeqId;
        private Action<GetRewardRet> _getRewardCallback;
        private Action<TaskProgressRet> _taskProgressCallback;
        private Action<TaskProgressListRet> _taskProgressListCallback;
        private Action<DriverDiskUpgradeRet> _driverDiskUpgradeCallback;
        private Action<DriverDiskEquipRet> _driverDiskEquipCallback;
        private Action<BagInfoRet> _bagInfoCallback;
        private Action<CreateRoomRet> _createRoomCallback;
        private Action<JoinRoomRet> _joinRoomCallback;
        private Action<RoomListRet> _roomListCallback;
        private Action<LeaveRoomRet> _leaveRoomCallback;
        private Action<FriendListRet> _friendListCallback;
        private Action<AddFriendRet> _addFriendCallback;
        private Action<HandleFriendRet> _handleFriendCallback;
        private Action<SendFriendMsgRet> _sendFriendMsgCallback;
        private Action<FriendHistoryRet> _friendHistoryCallback;
        private Action<RoomChatNtf> _roomChatCallback;

        #endregion

        #region 初始化

        // 场景/预制体里可能存在多个实例，只保留最早 Awake 的一个
        private static ProtoHandler _live;

        public void Init()
        {
            InitHandlers();
        }

        private void InitHandlers()
        {
            AppContext.Events.AddNetHandler(NetDefine.CMD_RegistCode, OnRegistResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_LoginCode, OnLoginResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_GetServerListCode, OnGetServerListResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_LoginGameServerCode, OnLoginGameServerResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_CreateRoleCode, OnCreateRoleResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_StartGameCode, OnStartGameResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_SaveRoleCode, OnSaveRoleResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_ChangeSceneCode, OnChangeSceneResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_SpawnEnemyCode, OnSpawnEnemyResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerAttackCode, OnPlayerAttackResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_GetRewardCode, OnGetRewardResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_TaskProgressCode, OnTaskProgressResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_TaskProgressReqCode, OnTaskProgressListResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_DriverDiskUpgradeCode, OnDriverDiskUpgradeResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_DriverDiskEquipCode, OnDriverDiskEquipResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_BagInfoCode, OnBagInfoResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_CreateRoomCode, OnCreateRoomResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_JoinRoomCode, OnJoinRoomResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_RoomListCode, OnRoomListResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_LeaveRoomCode, OnLeaveRoomResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_RoomInfoCode, OnRoomInfoNtf);
            AppContext.Events.AddNetHandler(NetDefine.CMD_RoomStartGameCode, OnRoomStartGameNtf);
            AppContext.Events.AddNetHandler(NetDefine.CMD_RoomChatNtfCode, OnRoomChatNtf);
            AppContext.Events.AddNetHandler(NetDefine.CMD_ErrCode, OnErrorResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_PositionSyncCode, OnPositionSync);
            AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerEnterSceneCode, OnPlayerEnterSceneEvent);
            AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerLeaveSceneCode, OnPlayerLeaveSceneEvent);
            AppContext.Events.AddNetHandler(NetDefine.CMD_SyneAniCode, OnSyncAniResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerVfxCode, OnPlayerVfxResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_EnemyPositionSyncCode, OnEnemyPosSyncResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_SyneEnemyAniCode, OnEnemyAniSyncResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerHurtCode, OnPlayerHurtResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_FriendListCode, OnFriendListResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_AddFriendCode, OnAddFriendResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_HandleFriendCode, OnHandleFriendResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_SendFriendMsgCode, OnSendFriendMsgResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_FriendHistoryCode, OnFriendHistoryResult);
            AppContext.Events.AddNetHandler(NetDefine.CMD_FriendApplyNtfCode, OnFriendApplyNtf);
            AppContext.Events.AddNetHandler(NetDefine.CMD_FriendMsgPushCode, OnFriendMsgPushNtf);
        }

        // 由 GameManager.Update 每帧驱动
        private const float SyncInterval = 0.05f; // 20 次/秒，位置同步足够平滑

        public void Tick()
        {
            _syncTimer += Time.deltaTime;
            if (!(_syncTimer >= SyncInterval)) return;
            _syncTimer = 0;

            // 玩家位置上报
            if (_syncTarget)
            {
                NetClientMgr.Instance.Send(NetDefine.CMD_PositionSyncCode, new PositionSyncReq
                {
                    RoleId = _syncRoleId,
                    PosX = _syncTarget.position.x,
                    PosY = _syncTarget.position.y,
                    PosZ = _syncTarget.position.z,
                    RotationY = _syncRotationTarget ? _syncRotationTarget.eulerAngles.y : 0f,
                    NikeName = _syncNickname
                }.ToByteString());
            }
        }

        #endregion

        #region 位置同步控制

        /// <summary>
        /// 开始定时向服务端发送位置信息（联机时在 StartGame 成功后调用）
        /// rotationTarget 用于同步模型旋转（playerModel.transform），不传则用 positionTarget 的旋转
        /// </summary>
        public void StartPositionSync(Transform positionTarget, int roleId, Transform rotationTarget = null)
        {
            MainRoleInfo info = AppContext.Session.MainRoleInfo;
            string nickname = info != null ? info.BaseInfo.Nickname : AppContext.Session.PlayerName;
            if (string.IsNullOrEmpty(nickname)) nickname = "Player" + roleId;

            _syncTarget = positionTarget;
            _syncRotationTarget = rotationTarget;
            _syncRoleId = roleId;
            _syncNickname = nickname;
            _syncTimer = 0;
        }

        public void StopPositionSync()
        {
            _syncTarget = null;
            _syncRotationTarget = null;
        }

        #endregion

        #region 请求方法

        // 玩家动画同步：单向广播（服务端只转发给其他人，不给请求者回包），fire-and-forget。
        public void RequestSyncAni(int roleId, string aniName, int skillConfigIndex)
        {
            SyncAniReq req = new SyncAniReq
                { RoleId = roleId, AnimationName = aniName, SkillConfigIndex = skillConfigIndex };
            NetClientMgr.Instance.Send(NetDefine.CMD_SyneAniCode, req.ToByteString());
        }

        // 敌人动画同步：单向广播，fire-and-forget。
        // 敌人动作同步不再由客户端上报：服务端 tick 决定动作并广播（见 OnEnemySyncAniReceived）。

        public void RequestSyncVfx(PlayerVfxNtf ntf)
        {
            NetClientMgr.Instance.Send(NetDefine.CMD_PlayerVfxCode, ntf.ToByteString());
        }

        public void RequestRegist(string userName, string phoneNum, string password, Action<RegistRet> callback)
        {
            _registCallback = callback;
            RegistReq req = new RegistReq { UserName = userName, Password = password };
            NetClientMgr.Instance.Send(NetDefine.CMD_RegistCode, req.ToByteString());
        }

        public void RequestLogin(string userName, string password, Action<loginRet> callback)
        {
            _loginCallback = callback;
            LoginReq req = new LoginReq { UserName = userName, Password = password };
            NetClientMgr.Instance.Send(NetDefine.CMD_LoginCode, req.ToByteString());
        }

        public void RequestServerList(Action<GetServerListRet> callback)
        {
            _serverListCallback = callback;
            GetServerListReq req = new GetServerListReq { ServerId = 0 };
            NetClientMgr.Instance.Send(NetDefine.CMD_GetServerListCode, req.ToByteString());
        }

        public void RequestLoginGameServer(int accountId, int serverId, Action<loginGameServerRet> callback)
        {
            _loginGameServerCallback = callback;
            LoginGameServerReq req = new LoginGameServerReq { AccountId = accountId, GameServerId = serverId };
            NetClientMgr.Instance.Send(NetDefine.CMD_LoginGameServerCode, req.ToByteString());
        }

        public void RequestCreateRole(int accountId, int serverId, string nickname, int jobId,
            Action<CreateRoleRet> callback)
        {
            _createRoleCallback = callback;
            CreateRoleReq req = new CreateRoleReq
            {
                AccountId = accountId,
                GameServerId = serverId,
                Nickname = nickname,
                JobId = jobId
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_CreateRoleCode, req.ToByteString());
        }

        public void RequestStartGame(int roleId, Action<StartGameRet> callback)
        {
            _startGameCallback = callback;
            StartGameReq req = new StartGameReq { RoleId = roleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_StartGameCode, req.ToByteString());
        }

        public void RequestSaveRole(SaveRoleReq req, Action<SaveRoleRet> callback)
        {
            _saveRoleCallback = callback;
            NetClientMgr.Instance.Send(NetDefine.CMD_SaveRoleCode, req.ToByteString());
        }

        public void RequestChangeScene(int roleId, string sceneName, Action<ChangeSceneRet> callback)
        {
            _changeSceneCallback = callback;
            ChangeSceneReq req = new ChangeSceneReq { RoleId = roleId, SceneName = sceneName };
            NetClientMgr.Instance.Send(NetDefine.CMD_ChangeSceneCode, req.ToByteString());
        }

        public void RequestSpawnEnemy(int roleId, int enemyConfigId, float maxHp, Vector3 pos,
            Action<SpawnEnemyRet> callback)
        {
            _spawnEnemyCallback = callback;
            SpawnEnemyReq req = new SpawnEnemyReq
            {
                RoleId = roleId,
                EnemyConfigId = enemyConfigId,
                MaxHp = maxHp,
                PosX = pos.x,
                PosY = pos.y,
                PosZ = pos.z
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_SpawnEnemyCode, req.ToByteString());
        }

        public void RequestPlayerAttack(int roleId, int enemyInstanceId, float damage, float baojiPercent,
            bool isExAttack,
            Action<PlayerAttackRet> callback)
        {
            int seqId = ++_nextAttackSeqId;
            _playerAttackCallbacks[seqId] = callback;
            PlayerAttackReq req = new PlayerAttackReq
            {
                RoleId = roleId,
                EnemyInstanceId = enemyInstanceId,
                Damage = damage,
                BaojiPercent = baojiPercent,
                IsExAttack = isExAttack,
                SequenceId = seqId
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_PlayerAttackCode, req.ToByteString());
        }

        /// <summary>
        /// 请求发放任务奖励。奖励内容与「能否领奖」都由服务端按 task_id 从 Luban 配置判定，客户端只上报领奖意图。
        /// </summary>
        public void RequestGetReward(int taskId, Action<GetRewardRet> callback)
        {
            _getRewardCallback = callback;
            GetRewardReq req = new GetRewardReq
            {
                RoleId = AppContext.Session.RoleId,
                RewardType = 1, // 1=任务奖励
                TaskId = taskId
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_GetRewardCode, req.ToByteString());
        }

        /// <summary>
        /// 保存任务进度到服务端（任务状态机迁移时调用）。批量上报，支持一次提交多个任务。
        /// </summary>
        public void RequestSaveTaskProgress(TaskDataRuntime task, Action<TaskProgressRet> callback)
        {
            _taskProgressCallback = callback;
            TaskProgressNtf ntf = new TaskProgressNtf { RoleId = AppContext.Session.RoleId };
            ntf.ProgressList.Add(new TaskProgressData
            {
                TaskId = task.TaskId,
                State = (int)task.State,
                CurrentCount = task.CurrentCount
            });
            NetClientMgr.Instance.Send(NetDefine.CMD_TaskProgressCode, ntf.ToByteString());
        }

        /// <summary>
        /// 从服务端拉取全量任务（定义 + 进度）。任务定义以服务端 Luban 配置为权威，客户端不再有本地任务配置。
        /// </summary>
        public void RequestLoadTaskProgress(Action<TaskProgressListRet> callback)
        {
            _taskProgressListCallback = callback;
            TaskProgressReq req = new TaskProgressReq { RoleId = AppContext.Session.RoleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_TaskProgressReqCode, req.ToByteString());
        }

        /// <summary>
        /// 请求升级驱动盘。只上报「升级哪个驱动盘」——材料够不够、扣多少、升级结果全部由服务端判定并落库。
        /// </summary>
        public void RequestDriverDiskUpgrade(int depotId, Action<DriverDiskUpgradeRet> callback)
        {
            _driverDiskUpgradeCallback = callback;
            DriverDiskUpgradeReq req = new DriverDiskUpgradeReq
            {
                RoleId = AppContext.Session.RoleId,
                DepotId = depotId
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_DriverDiskUpgradeCode, req.ToByteString());
        }

        /// <summary>请求装备 / 卸下驱动盘（状态由服务端写 role_bag_item.IsEquipped）</summary>
        public void RequestDriverDiskEquip(int depotId, bool equip, Action<DriverDiskEquipRet> callback)
        {
            _driverDiskEquipCallback = callback;
            DriverDiskEquipReq req = new DriverDiskEquipReq
            {
                RoleId = AppContext.Session.RoleId,
                DepotId = depotId,
                Equip = equip
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_DriverDiskEquipCode, req.ToByteString());
        }

        /// <summary>拉取角色全量背包（进游戏 / 领奖后刷新本地）</summary>
        public void RequestBagInfo(Action<BagInfoRet> callback)
        {
            _bagInfoCallback = callback;
            BagInfoReq req = new BagInfoReq { RoleId = AppContext.Session.RoleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_BagInfoCode, req.ToByteString());
        }

        /// <summary>拉取好友列表</summary>
        public void RequestFriendList(Action<FriendListRet> callback)
        {
            _friendListCallback = callback;
            FriendListReq req = new FriendListReq { RoleId = AppContext.Session.RoleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_FriendListCode, req.ToByteString());
        }

        /// <summary>发起好友申请</summary>
        public void RequestAddFriend(int fromRoleId, int toRoleId, Action<AddFriendRet> callback)
        {
            _addFriendCallback = callback;
            AddFriendReq req = new AddFriendReq { FromRoleId = fromRoleId, ToRoleId = toRoleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_AddFriendCode, req.ToByteString());
        }

        /// <summary>处理好友申请（同意/拒绝）</summary>
        public void RequestHandleFriend(int roleId, int targetRoleId, bool accept, Action<HandleFriendRet> callback)
        {
            _handleFriendCallback = callback;
            HandleFriendReq req = new HandleFriendReq
            {
                RoleId = roleId,
                TargetRoleId = targetRoleId,
                Accept = accept
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_HandleFriendCode, req.ToByteString());
        }

        /// <summary>发送私聊消息</summary>
        public void RequestSendFriendMsg(int receiverRoleId, string content, Action<SendFriendMsgRet> callback)
        {
            _sendFriendMsgCallback = callback;
            SendFriendMsgReq req = new SendFriendMsgReq
            {
                SenderRoleId = AppContext.Session.RoleId,
                ReceiverRoleId = receiverRoleId,
                Content = content
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_SendFriendMsgCode, req.ToByteString());
        }

        /// <summary>拉取与某好友的聊天记录（最多 100 条）</summary>
        public void RequestFriendHistory(int friendRoleId, Action<FriendHistoryRet> callback)
        {
            _friendHistoryCallback = callback;
            FriendHistoryReq req = new FriendHistoryReq
            {
                RoleId = AppContext.Session.RoleId,
                FriendRoleId = friendRoleId
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_FriendHistoryCode, req.ToByteString());
        }

        /// <summary>
        /// 上报玩家战斗属性。属性由客户端算（基础值 + 已装备驱动盘加成），但敌人攻击的伤害结算在服务端，
        /// 所以要把血量上限与防御同步过去。进游戏时一次，之后每次属性重算（换装 / 升级）再报一次。
        /// </summary>
        public void RequestPlayerStats(float maxHp, float defense)
        {
            PlayerStatsNtf ntf = new PlayerStatsNtf
            {
                RoleId = AppContext.Session.RoleId,
                MaxHp = maxHp,
                Defense = defense
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_PlayerStatsCode, ntf.ToByteString());
        }

        public void RequestCreateRoom(int roleId, string roomName, string nickname,
            Action<CreateRoomRet> callback)
        {
            _createRoomCallback = callback;
            CreateRoomReq req = new CreateRoomReq
            {
                RoleId = roleId,
                RoomName = roomName,
                Nickname = nickname
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_CreateRoomCode, req.ToByteString());
        }

        public void RequestJoinRoom(int roleId, int roomId, string nickname,
            Action<JoinRoomRet> callback)
        {
            _joinRoomCallback = callback;
            JoinRoomReq req = new JoinRoomReq
            {
                RoleId = roleId,
                RoomId = roomId,
                Nickname = nickname
            };
            NetClientMgr.Instance.Send(NetDefine.CMD_JoinRoomCode, req.ToByteString());
        }

        /// <summary>拉取房间列表（房间在 LoginServer 内存里，本地直接返回）</summary>
        public void RequestRoomList(Action<RoomListRet> callback)
        {
            _roomListCallback = callback;
            RoomListReq req = new RoomListReq { RoleId = AppContext.Session.RoleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_RoomListCode, req.ToByteString());
        }

        /// <summary>离开房间（服务端离开后回包，客户端据此关闭房间界面）</summary>
        public void RequestLeaveRoom(int roleId, Action<LeaveRoomRet> callback = null)
        {
            _leaveRoomCallback = callback;
            LeaveRoomReq req = new LeaveRoomReq { RoleId = roleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_LeaveRoomCode, req.ToByteString());
        }

        /// <summary>发送房间内聊天（服务端补昵称 / 时间后广播给房间内所有人）</summary>
        public void RequestRoomChat(int roleId, string content, Action<RoomChatNtf> callback)
        {
            _roomChatCallback = callback;
            RoomChatReq req = new RoomChatReq { RoleId = roleId, Content = content };
            NetClientMgr.Instance.Send(NetDefine.CMD_RoomChatCode, req.ToByteString());
        }

        public void RequestRoomStartGame(int roleId)
        {
            RoomStartGameReq req = new RoomStartGameReq { RoleId = roleId };
            NetClientMgr.Instance.Send(NetDefine.CMD_RoomStartGameCode, req.ToByteString());
        }

        public void RequestPlayerReady(int roleId, bool isReady)
        {
            PlayerReadyReq req = new PlayerReadyReq { RoleId = roleId, IsReady = isReady };
            NetClientMgr.Instance.Send(NetDefine.CMD_PlayerReadyCode, req.ToByteString());
        }

        #endregion

        #region 响应处理

        private void OnRegistResult(ByteString data)
        {
            RegistRet ret = RegistRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 注册结果 CmdCode={ret.CmdCode}");
            _registCallback?.Invoke(ret);
            _registCallback = null;
        }

        private void OnLoginResult(ByteString data)
        {
            loginRet ret = loginRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 登录结果 CmdCode={ret.CmdCode}");
            _loginCallback?.Invoke(ret);
            _loginCallback = null;
        }

        private void OnGetServerListResult(ByteString data)
        {
            GetServerListRet ret = GetServerListRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 服务器列表 数量={ret.GameServers.Count}");
            _serverListCallback?.Invoke(ret);
            _serverListCallback = null;
        }

        private void OnLoginGameServerResult(ByteString data)
        {
            loginGameServerRet ret = loginGameServerRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 登录游戏服务器结果 CmdCode={ret.CmdCode}");
            _loginGameServerCallback?.Invoke(ret);
            _loginGameServerCallback = null;
        }

        private void OnCreateRoleResult(ByteString data)
        {
            CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 创建角色结果 CmdCode={ret.CmdCode}");
            _createRoleCallback?.Invoke(ret);
            _createRoleCallback = null;
        }

        private void OnStartGameResult(ByteString data)
        {
            StartGameRet ret = StartGameRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 开始游戏结果 CmdCode={ret.CmdCode}");
            _startGameCallback?.Invoke(ret);
            _startGameCallback = null;
        }

        private void OnSaveRoleResult(ByteString data)
        {
            SaveRoleRet ret = SaveRoleRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 保存角色结果 CmdCode={ret.CmdCode}");
            _saveRoleCallback?.Invoke(ret);
            _saveRoleCallback = null;
        }

        private void OnChangeSceneResult(ByteString data)
        {
            ChangeSceneRet ret = ChangeSceneRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 切换场景结果 CmdCode={ret.CmdCode}");
            _changeSceneCallback?.Invoke(ret);
            _changeSceneCallback = null;
        }

        private void OnSpawnEnemyResult(ByteString data)
        {
            SpawnEnemyRet ret = SpawnEnemyRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 敌人生成结果 instanceId={ret.EnemyInstanceId} pos=({ret.PosX},{ret.PosY},{ret.PosZ})");

            if (_spawnEnemyCallback != null)
            {
                // 请求者的回调（GameManager.SpawnEnemy）
                _spawnEnemyCallback.Invoke(ret);
                _spawnEnemyCallback = null;
            }
            else
            {
                // 服务端广播给其他人 → 直接生成敌人
                AppContext.Enemy.SpawnEnemy(ret);
            }
        }

        private void OnPlayerAttackResult(ByteString data)
        {
            PlayerAttackRet ret = PlayerAttackRet.Parser.ParseFrom(data);
            Debug.Log(
                $"ProtoHandler: 攻击结果 attacker={ret.AttackerRoleId} enemy={ret.EnemyInstanceId} damage={ret.DamageDealt} isDead={ret.IsDead}");

            // 判断是不是自己的攻击（回包的 attacker_role_id = 本地 roleId，且请求中有匹配的回调）
            if (ret.AttackerRoleId == AppContext.Session.RoleId)
            {
                // 遍历字典找到匹配的回调（sequence_id 无法回传因为请求中没带）
                // 取字典中最旧的未处理回调
                if (_playerAttackCallbacks.Count > 0)
                {
                    var keys = new List<int>(_playerAttackCallbacks.Keys);
                    keys.Sort();
                    int oldestKey = keys[0];
                    var cb = _playerAttackCallbacks[oldestKey];
                    _playerAttackCallbacks.Remove(oldestKey);
                    cb?.Invoke(ret);
                }
            }
            else if (ret.AttackerRoleId > 0)
            {
                // 别人的攻击广播
                OnPlayerAttackBroadcast?.Invoke(ret);
            }
        }

        private void OnGetRewardResult(ByteString data)
        {
            GetRewardRet ret = GetRewardRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 奖励结果 CmdCode={ret.CmdCode}");
            _getRewardCallback?.Invoke(ret);
            _getRewardCallback = null;
        }

        private void OnTaskProgressResult(ByteString data)
        {
            TaskProgressRet ret = TaskProgressRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 任务进度保存结果 CmdCode={ret.CmdCode}");
            _taskProgressCallback?.Invoke(ret);
            _taskProgressCallback = null;
        }

        private void OnTaskProgressListResult(ByteString data)
        {
            TaskProgressListRet ret = TaskProgressListRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 任务拉取结果 CmdCode={ret.CmdCode} count={ret.TaskList.Count}");
            _taskProgressListCallback?.Invoke(ret);
            _taskProgressListCallback = null;
        }

        private void OnDriverDiskUpgradeResult(ByteString data)
        {
            DriverDiskUpgradeRet ret = DriverDiskUpgradeRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 驱动盘升级结果 CmdCode={ret.CmdCode} level={ret.OldLevel}->{ret.DriverDisk?.Level}");
            _driverDiskUpgradeCallback?.Invoke(ret);
            _driverDiskUpgradeCallback = null;
        }

        private void OnDriverDiskEquipResult(ByteString data)
        {
            DriverDiskEquipRet ret = DriverDiskEquipRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 驱动盘装备结果 CmdCode={ret.CmdCode} depotId={ret.DepotId} equipped={ret.Equipped}");
            _driverDiskEquipCallback?.Invoke(ret);
            _driverDiskEquipCallback = null;
        }

        private void OnBagInfoResult(ByteString data)
        {
            BagInfoRet ret = BagInfoRet.Parser.ParseFrom(data);
            Debug.Log(
                $"ProtoHandler: 背包拉取结果 CmdCode={ret.CmdCode} 驱动盘={ret.DriverDiskMap.Count} 材料={ret.MaterialMap.Count}");
            _bagInfoCallback?.Invoke(ret);
            _bagInfoCallback = null;
        }

        private void OnFriendListResult(ByteString data)
        {
            FriendListRet ret = FriendListRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 好友列表结果 CmdCode={ret.CmdCode} 好友数={ret.Friends.Count}");
            _friendListCallback?.Invoke(ret);
            _friendListCallback = null;
        }

        private void OnAddFriendResult(ByteString data)
        {
            AddFriendRet ret = AddFriendRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 添加好友结果 CmdCode={ret.CmdCode} tip={ret.Tip}");
            _addFriendCallback?.Invoke(ret);
            _addFriendCallback = null;
        }

        private void OnHandleFriendResult(ByteString data)
        {
            HandleFriendRet ret = HandleFriendRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 处理好友申请结果 CmdCode={ret.CmdCode}");
            _handleFriendCallback?.Invoke(ret);
            _handleFriendCallback = null;
        }

        private void OnSendFriendMsgResult(ByteString data)
        {
            SendFriendMsgRet ret = SendFriendMsgRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 发送私聊结果 CmdCode={ret.CmdCode} id={ret.Message?.Id}");
            _sendFriendMsgCallback?.Invoke(ret);
            _sendFriendMsgCallback = null;
        }

        private void OnFriendHistoryResult(ByteString data)
        {
            FriendHistoryRet ret = FriendHistoryRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 聊天记录结果 CmdCode={ret.CmdCode} 条数={ret.Messages.Count}");
            _friendHistoryCallback?.Invoke(ret);
            _friendHistoryCallback = null;
        }

        // 服务端推送：直接交给 FriendService 落地（不再经事件中转）
        private void OnFriendApplyNtf(ByteString data)
        {
            FriendApplyNtf ntf = FriendApplyNtf.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 好友申请通知 from={ntf.FromRoleId} nickname={ntf.FromNickname}");
            AppContext.Ui.ShowTip($"收到好友申请：{ntf.FromNickname}");
            // 面板开着就立刻重拉，把申请顶到列表最前面；没开则下次 Show() 时会拉到
            AppContext.Ui.GetPanel<FriendPanel>()?.OnFriendApplyReceived();
        }

        private void OnFriendMsgPushNtf(ByteString data)
        {
            FriendMsgPushNtf ntf = FriendMsgPushNtf.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 私聊推送 id={ntf.Message?.Id} from={ntf.Message?.SenderRoleId}");
            OnServerPushMessageReceived?.Invoke(ntf);
        }

        private void OnCreateRoomResult(ByteString data)
        {
            CreateRoomRet ret = CreateRoomRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 创建房间结果 roomId={ret.RoomId}");
            _createRoomCallback?.Invoke(ret);
            _createRoomCallback = null;
        }

        private void OnJoinRoomResult(ByteString data)
        {
            JoinRoomRet ret = JoinRoomRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 加入房间结果 roomId={ret.RoomId} players={ret.Players.Count}");
            _joinRoomCallback?.Invoke(ret);
            _joinRoomCallback = null;
        }

        private void OnRoomListResult(ByteString data)
        {
            RoomListRet ret = RoomListRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 房间列表结果 CmdCode={ret.CmdCode} 房间数={ret.Rooms.Count}");
            _roomListCallback?.Invoke(ret);
            _roomListCallback = null;
        }

        private void OnLeaveRoomResult(ByteString data)
        {
            LeaveRoomRet ret = LeaveRoomRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 离开房间结果 CmdCode={ret.CmdCode} tip={ret.Tip}");
            _leaveRoomCallback?.Invoke(ret);
            _leaveRoomCallback = null;
        }

        // 房间信息 / 开局 / 聊天都是服务端推给房间内的推送，直接交给当前打开的房间面板
        // （面板没开 = 人不在房间界面，丢弃即可；下次打开会重新拉列表）
        private void OnRoomInfoNtf(ByteString data)
        {
            RoomInfoNtf ntf = RoomInfoNtf.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 房间信息变更 roomId={ntf.RoomId} players={ntf.Players.Count}");
            AppContext.Ui.GetPanel<RoomPanel>()?.OnRoomInfoChanged(ntf);
        }

        private void OnRoomStartGameNtf(ByteString data)
        {
            RoomStartGameNtf ntf = RoomStartGameNtf.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 房间开始游戏 roomId={ntf.RoomId}");
            AppContext.Ui.ClosePanel<RoomPanel>();
            AppContext.Scene.LoadScene("GameScene");
        }

        private void OnRoomChatNtf(ByteString data)
        {
            RoomChatNtf ntf = RoomChatNtf.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 房间聊天信息 message = {ntf.Message}");
            _roomChatCallback?.Invoke(ntf);
        }

        private void OnErrorResult(ByteString data)
        {
            ErrMsg err = ErrMsg.Parser.ParseFrom(data);
            Debug.LogError($"ProtoHandler: 收到错误 CmdCode={err.CmdCode}");
        }

        private void OnSyncAniResult(ByteString data)
        {
            SyncAniRet ret = SyncAniRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 同步动画 ：{ret.AnimationName}");
            OnSyncAniReceived?.Invoke(ret);
        }

        private void OnPlayerVfxResult(ByteString data)
        {
            PlayerVfxNtf ntf = PlayerVfxNtf.Parser.ParseFrom(data);
            Debug.Log(
                $"ProtoHandler: VFX同步 roleId={ntf.RoleId} skill={ntf.SkillConfigIndex} atk={ntf.AttackIndex} vfx={ntf.VfxIndex}");
            OnPlayerVfxReceived?.Invoke(ntf);
        }

        private void OnEnemyPosSyncResult(ByteString data)
        {
            EnemyPositionSyncRet ntf = EnemyPositionSyncRet.Parser.ParseFrom(data);
            OnEnemyPositionSyncReceived?.Invoke(ntf);
        }

        private void OnEnemyAniSyncResult(ByteString data)
        {
            EnemySyncAniRet ret = EnemySyncAniRet.Parser.ParseFrom(data);
            Debug.Log($"ProtoHandler: 同步动画 ：{ret.AnimationName}");
            OnEnemySyncAniReceived?.Invoke(ret);
        }

        private void OnPlayerHurtResult(ByteString data)
        {
            PlayerHurtNtf ntf = PlayerHurtNtf.Parser.ParseFrom(data);
            Debug.Log(
                $"ProtoHandler: 玩家受击 roleId={ntf.RoleId} damage={ntf.Damage} hp={ntf.CurrHp}/{ntf.MaxHp} isDead={ntf.IsDead}");
            OnPlayerHurtReceived?.Invoke(ntf);
        }

        #endregion

        #region 位置同步 / 场景事件

        private void OnPositionSync(ByteString data)
        {
            PositionSyncNtf ntf = PositionSyncNtf.Parser.ParseFrom(data);
            OnPositionSyncReceived?.Invoke(ntf);
        }

        private void OnPlayerEnterSceneEvent(ByteString data)
        {
            PlayerEnterSceneNtf ntf = PlayerEnterSceneNtf.Parser.ParseFrom(data);
            OnPlayerEnterScene?.Invoke(ntf);
        }

        private void OnPlayerLeaveSceneEvent(ByteString data)
        {
            PlayerLeaveSceneNtf ntf = PlayerLeaveSceneNtf.Parser.ParseFrom(data);
            OnPlayerLeaveScene?.Invoke(ntf);
        }

        #endregion

        public void Clear()
        {
            // AppContext 可能已先一步 Dispose（退出流程），此时事件总线随它一起没了
            if (!AppContext.IsAlive) return;

            AppContext.Events.RemoveNetHandler(NetDefine.CMD_RegistCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_LoginCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_GetServerListCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_LoginGameServerCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_CreateRoleCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_StartGameCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_SaveRoleCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_ChangeSceneCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_SpawnEnemyCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerAttackCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_GetRewardCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_TaskProgressCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_TaskProgressReqCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_DriverDiskUpgradeCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_DriverDiskEquipCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_BagInfoCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_CreateRoomCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_JoinRoomCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_RoomListCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_LeaveRoomCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_RoomInfoCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_RoomStartGameCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_RoomChatNtfCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_ErrCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_PositionSyncCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerEnterSceneCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerLeaveSceneCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_SyneAniCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerVfxCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_EnemyPositionSyncCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_SyneEnemyAniCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerHurtCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_FriendListCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_AddFriendCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_HandleFriendCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_SendFriendMsgCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_FriendHistoryCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_FriendApplyNtfCode);
            AppContext.Events.RemoveNetHandler(NetDefine.CMD_FriendMsgPushCode);
        }
    }
}