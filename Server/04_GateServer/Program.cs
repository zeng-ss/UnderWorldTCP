using System.Threading;

namespace _04_GateServer
{
    internal class GateApp
    {
        public static void Main()
        {
            //网关服务器客户端   连接游戏服务器
            NetClient client = new NetClient(NetDefine.IPHost, NetDefine.GameServerPort, ClientType.GateServer);
            client.StartConnect();

            //网关服务器   开启服务端
            NetServer server = new NetServer(client);
            server.StartServer(NetDefine.IPHost, NetDefine.GateServerPort);

            Gate_LoginCtrl loginCtrl = new Gate_LoginCtrl();
            //可能还有其他的Ctrl

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

            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}