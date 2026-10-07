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
            LubanMgr.Instance.Init(); //初始化 luban（技能表 + 任务配置表读进内存，供服务端校验用）

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

            //RoleTable role = db.Queryable<RoleTable>().Where(v => v.Id == 1).First();
            /*var allRoles = db.Queryable<RoleTable>().ToList().Where(role => role.SceneName != "StartScene");
            foreach (var role in allRoles)
            {
                // 更新角色所在场景
                role.SceneName = "StartScene";
                role.UpdateDate = DateTime.Now;
                if (db.Updateable(role).ExecuteCommand() > 0)
                {
                    LogMsg.Info("场景初始化成功！");
                }
            }*/

            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}