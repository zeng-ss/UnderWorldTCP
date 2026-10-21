using System.Diagnostics;
using System.Threading;

namespace LoginServer
{
    internal class LoginApp
    {
        private static void Main()
        {
            // 敌人表（血量 / 攻击 / 移动 / 掉落）在登录服也要用：刷怪与击杀结算都在这边
            LubanMgr.Instance.Init();

            //登录服务器   连接中心服务器  
            NetClient client = new NetClient(NetDefine.IPHost, NetDefine.CenterServerPort, ClientType.LoginServer);
            client.StartConnect();

            //登录服务器   开启服务端  
            NetServer server = new NetServer(client);
            server.StartServer(NetDefine.IPHost, NetDefine.LoginServerPort);

            //注册指令集 
            LoginCtrl loginCtrl = new LoginCtrl();
            loginCtrl.OnInit();
            server.RegistCommand(NetDefine.CMD_RegistCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_LoginCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SaveRoleCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_ChangeSceneCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerAttackCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_GetRewardCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SpawnEnemyCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_TaskProgressCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_TaskProgressReqCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_DriverDiskUpgradeCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_DriverDiskEquipCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_BagInfoCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerStatsCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PositionSyncCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_CreateRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_JoinRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_LeaveRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_RoomStartGameCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerReadyCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_RoomListCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_RoomChatCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_KickPlayerCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SyneAniCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerVfxCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_FriendListCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_AddFriendCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_HandleFriendCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SendFriendMsgCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_FriendHistoryCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SetAvatarCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_FriendMsgReadCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_BackToLoginCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_ExitGameRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_EnemyKillCode, loginCtrl);

            client.RegistCommand(NetDefine.CMD_RegistCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_LoginCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_SaveRoleCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_ChangeSceneCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_SpawnEnemyCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_PlayerAttackCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_GetRewardCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_TaskProgressCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_TaskProgressReqCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_DriverDiskUpgradeCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_DriverDiskEquipCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_BagInfoCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_FriendListCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_AddFriendCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_HandleFriendCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_SendFriendMsgCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_FriendHistoryCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_SetAvatarCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_FriendMsgReadCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_BackToLoginCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_ExitGameRoomCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_EnemyDropCode, loginCtrl);

            // new Timer(_ =>
            // {
            //     //模拟 发送数据给中心服务器 让其处理注册的信息   
            //     RegistReq req = new RegistReq()
            //     {
            //         UserName = "aaaaaa",
            //         PhoneNum = "13000000000",
            //         Password = "12345"
            //     };
            //     client.SendData(NetDefine.CMD_RegistCode, req.ToByteString());
            //
            // }, null, 5000, Timeout.Infinite);

            // 之后的 while(true) 里跑服务端逻辑帧（敌人 AI + 位置/动作广播）
            RunLogicTick();
        }

        /// <summary>
        /// 固定频率推进服务端逻辑。敌人没有 Unity 侧的 Update，所有「AI」都在这里用纯数学推进。
        /// </summary>
        private static void RunLogicTick()
        {
            const int tickIntervalMs = 1000 / NetDefine.ServerTickRate;
            Stopwatch watch = Stopwatch.StartNew();

            while (true)
            {
                long start = watch.ElapsedMilliseconds;

                EnemyMgr.Instance.Tick(1f / NetDefine.ServerTickRate);

                int elapsed = (int)(watch.ElapsedMilliseconds - start);
                int sleep = tickIntervalMs - elapsed;
                Thread.Sleep(sleep > 0 ? sleep : 1);
            }
        }
    }
}