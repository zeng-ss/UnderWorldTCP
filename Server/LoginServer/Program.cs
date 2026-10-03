using System.Threading;

namespace LoginServer
{
    internal class LoginApp
    {
        private static void Main()
        {
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
            server.RegistCommand(NetDefine.CMD_PositionSyncCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_CreateRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_JoinRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_LeaveRoomCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_RoomStartGameCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerReadyCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SyneAniCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerVfxCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_EnemyPositionSyncCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SyneEnemyAniCode, loginCtrl);

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

            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}