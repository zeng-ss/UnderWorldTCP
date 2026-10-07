using System;
using System.Collections.Generic;
using SqlSugar;

/// <summary>
/// 处理登录模块相关的数据库业务类   详细处理数据相关的一些的操作  更新数据到数据库 或者从数据库中查询信息等  
/// </summary>
public class LoginModle
{
    private SqlSugarClient _db;

    public LoginModle(SqlSugarClient db)
    {
        _db = db;
    }

    #region 登录注册

    /// <summary>
    /// 注册账号   
    /// </summary>
    public RegistRet RegistAccount(RegistReq req)
    {
        RegistRet ret = new RegistRet(); //默认是0 0对应的就是成功   
        CmdCode cmdCode = CmdCode.Succeed;
        //1.判断 是否已经注册  
        List<AccountTable> accountTables =
            _db.Queryable<AccountTable>().Where(v => v.UserName == req.UserName).ToList();
        if (accountTables.Count > 0)
        {
            ret.CmdCode = CmdCode.AcctExist; //账号已存在  
        }
        else
        {
            //注册  
            AccountTable accountTable = new AccountTable
            {
                UserName = req.UserName,
                Password = req.Password,
                PhoneNum = "",
                LastLoginServerId = 1,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
            };
            //插入数据  
            int id = _db.Insertable(accountTable).ExecuteCommand();
            if (id <= 0) //说明执行失败  
            {
                ret.CmdCode = CmdCode.ServerError; //服务端错误 
            }
        }

        return ret; //返回信息码  
    }

    /// <summary>
    /// 登录处理
    /// </summary>
    public loginRet Login(LoginReq req)
    {
        loginRet ret = new loginRet();
        //获取匹配到的用户 
        AccountTable accountTable = _db.Queryable<AccountTable>().Where(v => v.UserName == req.UserName).First();
        if (accountTable == null)
        {
            ret.CmdCode = CmdCode.AcctNotExist; //账号不存在  
        }
        else
        {
            if (accountTable.Password.Equals(req.Password))
            {
                if (accountTable.State != 1)
                {
                    //账号被禁用  
                    ret.CmdCode = CmdCode.AcctDisable;
                }
                else
                {
                    //判断账号是否已经登录  
                    GameServerTable gameServerTable = _db.Queryable<GameServerTable>()
                        .Where(v => v.Id == accountTable.LastLoginServerId).First();
                    if (gameServerTable != null)
                    {
                        ret.GameServer = new GameServer
                        {
                            ServerId = gameServerTable.Id,
                            ServerName = gameServerTable.ServerName,
                            RunState = gameServerTable.RunState,
                            IsNew = gameServerTable.IsNew,
                            IpHost = gameServerTable.IPHost,
                            Port = gameServerTable.Port,
                        };
                    }

                    //登录成功  
                    ret.AccountId = accountTable.Id;
                }
            }
            else
            {
                //密码错误 
                ret.CmdCode = CmdCode.PasswordError;
            }
        }

        return ret; //返回错误码信息给客户端 
    }

    /// <summary>
    /// 从数据库中获取服务器列表 
    /// </summary>
    public GetServerListRet GetServerList(GetServerListReq req)
    {
        GetServerListRet ret = new GetServerListRet();
        if (req.ServerId == 0) //表示查询所有服务器的信息  
        {
            List<GameServerTable> gameServerTables = _db.Queryable<GameServerTable>().ToList();
            if (gameServerTables != null && gameServerTables.Count > 0)
            {
                foreach (var table in gameServerTables)
                {
                    GameServer gameServer = new GameServer
                    {
                        ServerId = table.Id,
                        ServerName = table.ServerName,
                        RunState = table.RunState,
                        IsNew = table.IsNew,
                        IpHost = table.IPHost,
                        Port = table.Port,
                    };
                    ret.GameServers.Add(gameServer);
                }
            }
            else
            {
                ret.CmdCode = CmdCode.ServerError;
            }
        }

        return ret;
    }

    /// <summary>
    /// 请求登录游戏服务器处理 
    /// </summary>
    public loginGameServerRet LoginGameServer(LoginGameServerReq req)
    {
        LogMsg.Info("[Center.DB]登录游戏服务器处理开始:accountId=" + req.AccountId + " serverId=" + req.GameServerId);
        loginGameServerRet ret = new loginGameServerRet();
        //查询当前登录的用户id    找到我们的角色表里面有没有创建过角色 账号id和服务器id同时匹配成功说明在某个服务器里面创建过该角色
        AccountTable accountTable = _db.Queryable<AccountTable>().Where(v => v.Id == req.AccountId).First();
        if (accountTable != null)
        {
            //拿到对应的服务器表里面的信息   
            GameServerTable gameServerTable =
                _db.Queryable<GameServerTable>().Where(v => v.Id == req.GameServerId).First();
            if (gameServerTable != null)
            {
                //根据查询到的服务器信息 来更新用户最后登录的服务器id  
                accountTable.LastLoginServerId = gameServerTable.Id;
                if (_db.Updateable(accountTable).ExecuteCommand() > 0)
                {
                    //将查询到的角色数据信息返回    拿到了服务器的信息  这里处理返回数据 需要有一个返回的数据类型  
                    RoleTable roleTable = _db.Queryable<RoleTable>()
                        .Where(v => v.AccountId == req.AccountId && v.ServerId == req.GameServerId).First();
                    if (roleTable != null)
                    {
                        //如果有说明已经创建过角色   没有创建则返回默认数据 
                        ret.CreateRoleInfo = new CreateRoleRet
                        {
                            RoleId = roleTable.Id,
                            Nickname = roleTable.NickName,
                        };
                    }
                }
                else ret.CmdCode = CmdCode.ServerError;
            }
            else ret.CmdCode = CmdCode.ReqParamError; //请求参数错误 
        }
        else ret.CmdCode = CmdCode.AcctNotExist;

        return ret;
    }

    #endregion

    /// <summary>
    /// 处理创建角色返回数据
    /// </summary>
    public CreateRoleRet CreateRole(CreateRoleReq req)
    {
        CreateRoleRet ret = new CreateRoleRet();
        //检查在同个服务器里面是否出现同名的情况 
        RoleTable roleTable = _db.Queryable<RoleTable>()
            .Where(v => v.ServerId == req.GameServerId && v.NickName == req.Nickname).First();
        if (roleTable != null)
        {
            //昵称已经存在  
            ret.CmdCode = CmdCode.NicknameExist;
        }
        else
        {
            RoleTable newRole = new RoleTable
            {
                //这里我们可以定义一些默认角色的数据然后从默认数据中加载就可以了 
                Id = req.AccountId,
                AccountId = req.AccountId,
                NickName = req.Nickname,
                SceneName = "StartScene",
                ServerId = req.GameServerId, //角色创建所属的服务器 
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
            };
            //将创建的数据插入到表中
            if (_db.Insertable(newRole).ExecuteCommand() > 0)
            {
                ret.RoleId = newRole.Id;
                ret.Nickname = newRole.NickName;

                //初始化角色任务进度：遍历 Luban 任务配置，为每个任务写入初始状态
                //（默认解锁的进入 InProgress，否则 Locked），使 role_task_progress 持有该角色所有任务的状态
                InitRoleTaskProgress(newRole.Id);
            }
            else
            {
                ret.CmdCode = CmdCode.ServerError;
            }
        }

        return ret;
    }

    /// <summary>
    /// 创建角色时初始化任务进度：把所有 Luban 任务配置写入 role_task_progress 表。
    /// 状态：IsUnlock 默认解锁 → InProgress(1)，否则 → Locked(0)。
    /// 幂等：若该角色已存在任一任务进度记录则跳过，避免重复插入。
    /// </summary>
    private void InitRoleTaskProgress(int roleId)
    {
        // 幂等保护：已有任务进度记录则跳过
        bool hasRecord = _db.Queryable<RoleTaskProgress>().Where(v => v.RoleId == roleId).Any();
        if (hasRecord)
        {
            return;
        }

        var taskDatas = LubanMgr.Instance.GetTaskDatas();
        if (taskDatas == null || taskDatas.Count == 0)
        {
            LogMsg.Info("InitRoleTaskProgress：Luban taskData 表为空，跳过初始化");
            return;
        }

        foreach (var kv in taskDatas)
        {
            var t = kv.Value;
            int state = t.IsUnlock ? 1 : 0; // 1=InProgress 0=Locked（对应 TaskState 枚举）
            _db.Insertable(new RoleTaskProgress
            {
                RoleId = roleId,
                TaskId = t.TaskID,
                State = state,
                CurrentCount = 0,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now
            }).ExecuteCommand();
        }

        LogMsg.Info("InitRoleTaskProgress：为角色 " + roleId + " 初始化 " + taskDatas.Count + " 条任务进度");
    }

    /// <summary>
    /// 服务端启动时的补偿逻辑：扫描所有已存在角色，为「在 role_task_progress 里没有任何任务记录」的角色补初始化。
    /// 保证老角色也能拿到任务配置对应的初始进度（幂等，不会重复插入）。
    /// </summary>
    public void CompensateRoleTaskProgress()
    {
        var taskDatas = LubanMgr.Instance.GetTaskDatas();
        if (taskDatas == null || taskDatas.Count == 0)
        {
            LogMsg.Info("CompensateRoleTaskProgress：Luban taskData 表为空，跳过补偿");
            return;
        }

        // 已存在任务进度记录的角色 id 集合（用于跳过）
        var progressList = _db.Queryable<RoleTaskProgress>().ToList();
        HashSet<int> initializedRoleIds = new HashSet<int>();
        foreach (var p in progressList)
        {
            initializedRoleIds.Add(p.RoleId);
        }

        // 所有角色
        var allRoles = _db.Queryable<RoleTable>().ToList();
        int compensated = 0;
        foreach (var role in allRoles)
        {
            if (initializedRoleIds.Contains(role.Id))
            {
                continue; // 已有任务进度，跳过
            }

            foreach (var kv in taskDatas)
            {
                var t = kv.Value;
                int state = t.IsUnlock ? 1 : 0;
                _db.Insertable(new RoleTaskProgress
                {
                    RoleId = role.Id,
                    TaskId = t.TaskID,
                    State = state,
                    CurrentCount = 0,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                }).ExecuteCommand();
            }

            compensated++;
        }

        LogMsg.Info("CompensateRoleTaskProgress：为 " + compensated + " 个角色补齐任务进度（总角色 " + allRoles.Count + " 个）");
    }

    /// <summary>
    /// 处理并返回开始游戏相关的数据  
    /// </summary>
    public StartGameRet StartGame(StartGameReq req)
    {
        StartGameRet ret = new StartGameRet();
        //根据传进来的角色id获取角色表中对应id的数据   
        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == req.RoleId).First();
        var roleBagInfo = _db.Queryable<RoleBagInfo>().Where(v => v.RoleId == req.RoleId).ToList();
        if (role != null)
        {
            role.SceneName = "StartScene";
            _db.Updateable(role).ExecuteCommand();
            //角色基础信息   创建角色的信息   到时候在其他窗口可能会创建一些怪物 怪物不是本地的 可能是服务端要求创建的    
            RoleBaseInfo baseInfo = new RoleBaseInfo
            {
                RoleId = role.Id,
                Nickname = role.NickName,
            };
            //主角信息  
            MainRoleInfo mainRoleInfo = new MainRoleInfo
            {
                BaseInfo = baseInfo,
                AccountId = role.AccountId,
                SceneName = role.SceneName,
                ServerId = role.ServerId
            };

            // 加载背包物品
            foreach (var bagInfo in roleBagInfo)
            {
                if (bagInfo.ItemType == "驱动盘")
                {
                    DriverDiskInfo diskInfo = new DriverDiskInfo
                    {
                        Level = bagInfo.Level,
                        DriverDiskId = bagInfo.ItemId,
                        DriverDiskCount = bagInfo.Count,
                        DriverDiskName = bagInfo.ItemName,
                        BaseValue = bagInfo.BaseValue,
                        AttackPer = bagInfo.AttackPercent,
                        DefensePer = bagInfo.DefensePercent,
                        BaoJiPer = bagInfo.BaoJiPercent,
                        CurFillValue = bagInfo.CurFillValue,
                        CurMaxFillValue = bagInfo.CurMaxFillValue
                    };
                    mainRoleInfo.DriverDiskMap.Add(bagInfo.ItemId, diskInfo);
                }
                else
                {
                    MaterialInfo materialInfo = new MaterialInfo
                    {
                        MaterialId = bagInfo.ItemId,
                        MaterialName = bagInfo.ItemName,
                        MaterialCount = bagInfo.Count
                    };
                    mainRoleInfo.MaterialMap.Add(bagInfo.ItemId, materialInfo);
                }
            }

            ret.MainRoleInfo = mainRoleInfo;
        }
        else ret.CmdCode = CmdCode.RoleNotExist; //角色不存在

        return ret;
    }

    /// <summary>
    /// 保存角色数据
    /// </summary>
    public SaveRoleRet SaveRole(SaveRoleReq req)
    {
        SaveRoleRet ret = new SaveRoleRet();
        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == req.RoleId).First();
        if (role == null)
        {
            ret.CmdCode = CmdCode.RoleNotExist;
            return ret;
        }

        role.UpdateDate = DateTime.Now;

        if (_db.Updateable(role).ExecuteCommand() <= 0) ret.CmdCode = CmdCode.ServerError;

        // 保存背包信息
        _db.Deleteable<RoleBagInfo>().Where(v => v.RoleId == req.RoleId).ExecuteCommand();

        return ret;
    }

    // 跳转场景
    public ChangeSceneRet ChangeScene(ChangeSceneReq req)
    {
        ChangeSceneRet ret = new ChangeSceneRet();
        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == req.RoleId).First();
        if (role == null)
        {
            ret.CmdCode = CmdCode.RoleNotExist;
            return ret;
        }

        // 验证场景跳转是否合法：根据当前所在场景，只能跳到指定的下一个场景
        bool valid = false;
        switch (role.SceneName)
        {
            case "StartScene":
                if (req.SceneName == "LobbyScene") valid = true;
                break;
            case "LobbyScene":
                if (req.SceneName == "GameScene") valid = true;
                break;
        }

        if (!valid)
        {
            LogMsg.Info($"[Center.DB]场景跳转校验失败:不能从{role.SceneName}跳转到{req.SceneName}！", ConsoleColor.Red);
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        // 更新角色所在场景
        role.SceneName = req.SceneName;
        role.UpdateDate = DateTime.Now;
        if (_db.Updateable(role).ExecuteCommand() <= 0) ret.CmdCode = CmdCode.ServerError;

        return ret;
    }
    
    /// <summary>
    /// 处理并返回获得奖励相关的数据  
    /// </summary>
    public GetRewardRet GetReward(GetRewardReq req)
    {
        GetRewardRet ret = new GetRewardRet();
        var nameMap = new Dictionary<int, string>
        {
            { 1, "混沌重金属" },
            { 2, "啄木鸟电音" },
            { 3, "原始朋克" },
            { 4, "灵魂摇滚" },
            { 5, "金币" },
            { 6, "攻击驱动" },
            { 7, "生命驱动" },
            { 8, "防御驱动" },
            { 9, "暴击驱动" }
        };
        // rewardMap: key=物品id, value=增加数量
        switch (req.RewardType)
        {
            case 1:
                ret.RewardMap.Add(1, 1);
                ret.RewardMap.Add(2, 1);
                ret.RewardMap.Add(3, 1);
                ret.RewardMap.Add(4, 1);
                ret.RewardMap.Add(5, 800);
                ret.RewardMap.Add(6, 20);
                ret.RewardMap.Add(7, 20);
                ret.RewardMap.Add(8, 20);
                ret.RewardMap.Add(9, 20);
                break;
            case 2:
                ret.RewardMap.Add(1, 1);
                ret.RewardMap.Add(2, 1);
                ret.RewardMap.Add(3, 1);
                ret.RewardMap.Add(4, 1);
                ret.RewardMap.Add(5, 1000);
                ret.RewardMap.Add(6, 20);
                ret.RewardMap.Add(7, 20);
                ret.RewardMap.Add(8, 20);
                ret.RewardMap.Add(9, 20);
                break;
        }

        // 已有物品数据
        var existingItems = _db.Queryable<RoleBagInfo>().Where(v => v.RoleId == req.RoleId).ToList();

        foreach (var kv in ret.RewardMap)
        {
            int itemId = kv.Key;
            int addCount = kv.Value;
            var exist = existingItems.Find(v => v.ItemId == itemId);
            if (exist != null)
            {
                exist.Count += addCount;
                exist.UpdateDate = DateTime.Now;
                _db.Updateable(exist).ExecuteCommand();
            }
            else
            {
                string itemType = itemId <= 4 ? "DriverDisk" : "Material";
                _db.Insertable(new RoleBagInfo
                {
                    RoleId = req.RoleId,
                    ItemId = itemId,
                    ItemType = itemType,
                    ItemName = nameMap[itemId],
                    Count = addCount,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                }).ExecuteCommand();
            }
        }

        return ret;
    }

    #region 任务进度持久化

    /// <summary>
    /// 保存任务进度（批量）：客户端任务状态机迁移 / 进度变化时上报。
    /// 采用「存在则更新、不存在则插入」的 upsert，避免重复记录。
    /// </summary>
    public TaskProgressRet SaveTaskProgress(TaskProgressNtf ntf)
    {
        TaskProgressRet ret = new TaskProgressRet();
        if (ntf.RoleId <= 0 || ntf.ProgressList == null || ntf.ProgressList.Count == 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        foreach (var p in ntf.ProgressList)
        {
            RoleTaskProgress exist = _db.Queryable<RoleTaskProgress>()
                .Where(v => v.RoleId == ntf.RoleId && v.TaskId == p.TaskId).First();
            if (exist != null)
            {
                exist.State = p.State;
                exist.CurrentCount = p.CurrentCount;
                exist.UpdateDate = DateTime.Now;
                _db.Updateable(exist).ExecuteCommand();
            }
            else
            {
                _db.Insertable(new RoleTaskProgress
                {
                    RoleId = ntf.RoleId,
                    TaskId = p.TaskId,
                    State = p.State,
                    CurrentCount = p.CurrentCount,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                }).ExecuteCommand();
            }
        }

        return ret;
    }

    /// <summary>
    /// 拉取角色的全量任务进度（登录 / 进入游戏时恢复任务状态机）。
    /// 服务端没有记录的返回空列表，客户端保持默认状态。
    /// </summary>
    public TaskProgressListRet LoadTaskProgress(TaskProgressReq req)
    {
        TaskProgressListRet ret = new TaskProgressListRet();
        if (req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        var list = _db.Queryable<RoleTaskProgress>().Where(v => v.RoleId == req.RoleId).ToList();
        foreach (var item in list)
        {
            ret.ProgressList.Add(new TaskProgressData
            {
                TaskId = item.TaskId,
                State = item.State,
                CurrentCount = item.CurrentCount
            });
        }

        return ret;
    }

    #endregion
}