using System.Threading;

namespace _03_GameServer
{
    internal class GameApp
    {
        public static void Main()
        {
            //游戏逻辑服务器客户端   连接中心服务器
            NetClient client = new NetClient(NetDefine.IPHost, NetDefine.CenterServerPort, ClientType.GameServer);
            client.StartConnect();

            //游戏逻辑服务器   开启服务端
            NetServer server = new NetServer(client);
            server.StartServer(NetDefine.IPHost, NetDefine.GameServerPort);

            Game_LoginCtrl loginCtrl = new Game_LoginCtrl();

            //注册指令集
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SaveRoleCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_ChangeSceneCode, loginCtrl);

            client.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_SaveRoleCode, loginCtrl);
            client.RegistCommand(NetDefine.CMD_ChangeSceneCode, loginCtrl);

            while (true) Thread.Sleep(1);
        }
    }
}