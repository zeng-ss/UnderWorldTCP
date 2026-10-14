using System.Threading;
using CenterServer.DB;
using SqlSugar;

namespace CenterServer
{
    //中心服务器主程序入口  
    internal class CenterApp
    {
        static void Main()
        {
            NetServer server = new NetServer();
            server.StartServer(NetDefine.IPHost, NetDefine.CenterServerPort);
            SqlSugarClient db = DBMgr.Instance.InitDB(); //初始化数据库 
            LubanMgr.Instance.Init(); //初始化 luban

            //创建负责和接收登录注册的模块对象 相关的数据  
            LoginModle loginModle = new LoginModle(db);
            Center_LoginCtrl loginCtrl = new Center_LoginCtrl(loginModle);
            //服务端启动时补偿初始化：为还没有任务进度记录的角色补齐所有任务的初始状态（幂等）
            loginModle.CompensateRoleTaskProgress();
            //注册指令集
            server.RegistCommand(NetDefine.CMD_RegistCode, loginCtrl); //注册接口指令集
            server.RegistCommand(NetDefine.CMD_LoginCode, loginCtrl); //登录接口指令集
            server.RegistCommand(NetDefine.CMD_GetServerListCode, loginCtrl); //获取服务器列表指令集
            server.RegistCommand(NetDefine.CMD_LoginGameServerCode, loginCtrl); //登录游戏服务器接口指令集
            server.RegistCommand(NetDefine.CMD_CreateRoleCode, loginCtrl); //创建角色接口指令集
            server.RegistCommand(NetDefine.CMD_StartGameCode, loginCtrl); //开始游戏接口指令集
            server.RegistCommand(NetDefine.CMD_SaveRoleCode, loginCtrl); //保存角色数据指令集
            server.RegistCommand(NetDefine.CMD_ChangeSceneCode, loginCtrl); //跳转场景指令集
            server.RegistCommand(NetDefine.CMD_GetRewardCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SpawnEnemyCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_PlayerAttackCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_TaskProgressCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_TaskProgressReqCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_DriverDiskUpgradeCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_DriverDiskEquipCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_BagInfoCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_FriendListCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_AddFriendCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_HandleFriendCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_SendFriendMsgCode, loginCtrl);
            server.RegistCommand(NetDefine.CMD_FriendHistoryCode, loginCtrl);

            /*
            var allRoles = db.Queryable<RoleBagInfo>().ToList();
            foreach (var roleBagInfo in allRoles)
            {
                if (roleBagInfo.ItemId < 5)
                {
                    var info = LubanMgr.Instance.GetDriverDiskById(roleBagInfo.ItemId);
                    roleBagInfo.BaseValue = info.BaseValue;
                    roleBagInfo.AttackPercent = info.AttackPercent;
                    roleBagInfo.DefensePercent = info.DefensePercent;
                    roleBagInfo.HealthPercent = info.HealthPercent;
                    roleBagInfo.BaoJiPercent = info.BaoJiPercent;
                    roleBagInfo.Level = 1;
                    roleBagInfo.CurMaxFillValue = 1000;
                    roleBagInfo.CurFillValue = 0;
                }

                if (db.Updateable(roleBagInfo).ExecuteCommand() > 0)
                {
                    LogMsg.Info("场景初始化成功！");
                }
            }
            */

            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}