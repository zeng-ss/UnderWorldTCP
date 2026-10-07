using SqlSugar;

namespace CenterServer.DB
{
    /// <summary>
    /// 数据库管理器 
    /// </summary>
    public class DBMgr : Singleton<DBMgr>
    {
        public SqlSugarClient InitDB()
        {
            ConnectionConfig connectionConfig = new ConnectionConfig
            {
                ConnectionString = "Server=localhost;Port=3306;DataBase=game;User=root;Password=123456;",
                DbType = DbType.MySql,
                IsAutoCloseConnection = true,
            };
            SqlSugarClient db = new SqlSugarClient(connectionConfig);

            //建库  
            db.DbMaintenance.CreateDatabase();
            //建表 用户信息表  服务器信息表    
            db.CodeFirst.InitTables(typeof(AccountTable), typeof(GameServerTable), typeof(RoleTable),
                typeof(RoleBagInfo), typeof(RoleTaskProgress));


            //给服务器信息表添加一个默认数据 这里就模拟创建20个服务器信息 
            /*for (int i = 0; i < 5; i++)
            {
                //创建数据对象
                GameServerTable gameServerTable = new GameServerTable
                {
                    ServerName = i + 1 + "区 镖人" + (i + 1) + "队",
                    RunState = 1,
                    IsNew = 1,
                    IPHost = NetDefine.IPHost,
                    Port = NetDefine.GateServerPort,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                };
                //往数据库中插入数据
                db.Insertable(gameServerTable).ExecuteCommand();
            }
            Console.WriteLine("80个服务器初始化成功！！");*/


            return db;
        }
    }
}