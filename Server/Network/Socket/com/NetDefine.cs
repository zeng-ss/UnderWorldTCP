public class NetDefine
{
    public const string IPHost = "127.0.0.1"; //本机IP
    public const int CenterServerPort = 10110; //中心服务器端口号
    public const int LoginServerPort = 10120; //登录服务器的端口号
    public const int GameServerPort = 10130; //游戏服务器的端口号
    public const int GateServerPort = 10140; //网关服务器的端口号
    public const ushort CMD_ErrCode = 10001; //错误码

    public const ushort CMD_RegistCode = 11010; //注册请求码
    public const ushort CMD_LoginCode = 11020; //登录请求码

    public const ushort CMD_GetServerListCode = 11030; //获取服务器列表请求码  
    public const ushort CMD_LoginGameServerCode = 11040; //登录服务器的请求码
    public const ushort CMD_CreateRoleCode = 11050; //创建角色请求码

    public const ushort CMD_StartGameCode = 11060; //开始游戏请求码

    public const ushort CMD_SaveRoleCode = 11070; //保存角色数据请求码
    public const ushort CMD_ChangeSceneCode = 11080; // 跳转场景请求码
    public const ushort CMD_SpawnEnemyCode = 11090; // 创建敌人请求码
    public const ushort CMD_PlayerAttackCode = 11100; // 玩家攻击请求码
    public const ushort CMD_GetRewardCode = 11110; // 获取奖励请求码

    public const ushort CMD_PositionSyncCode = 11120; // 位置同步
    public const ushort CMD_PlayerEnterSceneCode = 11130; // 玩家进入场景通知
    public const ushort CMD_PlayerLeaveSceneCode = 11140; // 玩家离开场景通知
    public const ushort CMD_CreateRoomCode = 11150; // 创建房间
    public const ushort CMD_JoinRoomCode = 11160; // 加入房间
    public const ushort CMD_LeaveRoomCode = 11170; // 离开房间
    public const ushort CMD_RoomStartGameCode = 11180; // 房主开始游戏
    public const ushort CMD_RoomInfoCode = 11190; // 房间信息广播通知
    public const ushort CMD_PlayerReadyCode = 11200; // 玩家准备状态切换
    public const ushort CMD_SyneAniCode = 11210;
    public const ushort CMD_PlayerVfxCode = 11230;
    public const ushort CMD_SyneEnemyAniCode = 11240;
    public const ushort CMD_EnemyPositionSyncCode = 11250; // 位置同步
    public const ushort CMD_TaskProgressCode = 11260; // 任务进度上报
    public const ushort CMD_TaskProgressReqCode = 11270; // 任务进度拉取

    //...定义新的一些指令
}


/// <summary>
/// 连接状态
/// </summary>
public enum ConnState
{
    Connected,
    Disconnected,
}


/// <summary>
/// 客户端类型
/// </summary>
public enum ClientType
{
    Unity,
    LoginServer,
    GameServer,
    GateServer,
}