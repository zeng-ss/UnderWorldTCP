using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
using cfg;

/// <summary>
/// 处理登录模块相关的数据库业务类   详细处理数据相关的一些的操作  更新数据到数据库 或者从数据库中查询信息等  
/// </summary>
public class LoginModle
{
    private readonly SqlSugarClient _db;

    // 装备槽上限
    private const int MaxEquipSlots = 5;

    // 每次强化投入的经验值（服务端权威，客户端不再自己算）
    private const int AddExpPerUpgrade = 200;

    // 升级词条的随机数。System.Random 不是线程安全的，统一加锁取数
    private static readonly Random Rng = new Random();
    private static readonly object RngLock = new object();

    // [minInclusive, maxExclusive) 之间的随机整数
    private static int NextRandom(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        lock (RngLock)
        {
            return Rng.Next(minInclusive, maxExclusive);
        }
    }

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
                PhoneNum = $"+86{req.Password}",
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
                            // 头像必须在这里带回：客户端登录后只拿到 RoleId / 昵称，
                            // 点「进入游戏」前的开始界面要显示头像，等 StartGame 就太晚了
                            Avatar = AvatarCatalog.Normalize(roleTable.Avatar),
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
                SceneName = "EnterScene",
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
            role.SceneName = "EnterScene";
            _db.Updateable(role).ExecuteCommand();
            //角色基础信息   创建角色的信息   到时候在其他窗口可能会创建一些怪物 怪物不是本地的 可能是服务端要求创建的    
            RoleBaseInfo baseInfo = new RoleBaseInfo
            {
                RoleId = role.Id,
                Nickname = role.NickName,
                Avatar = AvatarCatalog.Normalize(role.Avatar),
            };
            //主角信息  
            MainRoleInfo mainRoleInfo = new MainRoleInfo
            {
                BaseInfo = baseInfo,
                AccountId = role.AccountId,
                SceneName = role.SceneName,
                ServerId = role.ServerId
            };

            // 加载背包物品（驱动盘 / 材料统一走 ItemCatalog 分流，与发奖、升级同源）
            foreach (var bagInfo in roleBagInfo)
            {
                if (bagInfo.ItemType == ItemCatalog.DriverDiskType)
                {
                    mainRoleInfo.DriverDiskMap[bagInfo.ItemId] = ToDriverDiskInfo(bagInfo);
                }
                else
                {
                    mainRoleInfo.MaterialMap[bagInfo.ItemId] = ToMaterialInfo(bagInfo);
                }
            }

            ret.MainRoleInfo = mainRoleInfo;
        }
        else ret.CmdCode = CmdCode.RoleNotExist; //角色不存在

        return ret;
    }

    /// <summary>
    /// 设置头像：只校验编号范围，落库后把生效的编号回给客户端。
    /// </summary>
    public SetAvatarRet SetAvatar(SetAvatarReq req)
    {
        SetAvatarRet ret = new SetAvatarRet();
        if (req.RoleId <= 0 || !AvatarCatalog.IsValid(req.Avatar))
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        RoleTable role = _db.Queryable<RoleTable>().Where(v => v.Id == req.RoleId).First();
        if (role == null)
        {
            ret.CmdCode = CmdCode.RoleNotExist;
            return ret;
        }

        role.Avatar = req.Avatar;
        role.UpdateDate = DateTime.Now;
        if (_db.Updateable(role).ExecuteCommand() <= 0)
        {
            ret.CmdCode = CmdCode.ServerError;
            return ret;
        }

        ret.Avatar = role.Avatar;
        return ret;
    }

    /// <summary>
    /// 击杀掉落落库：登录服已经把「谁打的、掉什么」算好了，这里只写进背包并把最新数量回给客户端。
    /// </summary>
    public EnemyDropRet GrantEnemyDrops(EnemyDropReq req)
    {
        EnemyDropRet ret = new EnemyDropRet();
        if (req.RoleId <= 0 || req.Drops.Count == 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        // 同一种物品在掉落列表里重复出现时先合并，避免同一条记录被加两次
        Dictionary<int, int> rewardMap = new Dictionary<int, int>();
        foreach (DropItem drop in req.Drops)
        {
            if (drop.ItemId <= 0 || drop.Count <= 0) continue;
            rewardMap[drop.ItemId] = rewardMap.TryGetValue(drop.ItemId, out int old) ? old + drop.Count : drop.Count;
        }

        if (rewardMap.Count == 0)
        {
            ret.CmdCode = CmdCode.Succeed;
            return ret;
        }

        GrantToBag(req.RoleId, rewardMap);

        // 回「发放后」的最新数量：客户端直接覆盖
        List<RoleBagInfo> rows = _db.Queryable<RoleBagInfo>().Where(v => v.RoleId == req.RoleId).ToList();
        foreach (var kv in rewardMap)
        {
            RoleBagInfo row = rows.Find(v => v.ItemId == kv.Key);
            if (row != null) ret.MaterialCounts[kv.Key] = row.Count;
        }

        // 每条掉落一段文案，用 "/" 断开；敌人名字由客户端在生成敌人时已经拿到，
        // 这里只管「掉了什么」，表现层的事不下发到中心服这一层
        string desc = string.Empty;
        foreach (var kv in rewardMap)
        {
            if (desc.Length > 0) desc += "/";
            desc += $"{ItemCatalog.GetName(kv.Key)}×{kv.Value}";
        }

        ret.RewardDesc = desc;
        ret.CmdCode = CmdCode.Succeed;
        LogMsg.Info($"[Center]击杀掉落落库: roleId={req.RoleId} {desc}");
        return ret;
    }

    /// <summary>
    /// 退出登录：把角色的场景重置为主界面，下次登录不会直接落进 Lobby / Game。
    /// 在线列表在 LoginServer 侧维护，那边收到请求时就调 OnPlayerLeave 了。
    /// </summary>
    public BackToLoginRet BackToLogin(BackToLoginReq req)
    {
        BackToLoginRet ret = new BackToLoginRet();
        if (req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        _db.Updateable<RoleTable>()
            .SetColumns(v => v.SceneName == "EnterScene")
            .Where(v => v.Id == req.RoleId)
            .ExecuteCommand();

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 保存角色数据。
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

        return ret;
    }

    /// <summary>
    /// 处理并返回获得奖励相关的数据。
    /// 任务奖励（reward_type=1）：奖励内容与数量以服务端 Luban taskData 为权威，
    /// 按 task_id 取 depotIds/depotNums、materialsId/materialNums 发放；
    /// 发放明细写入 role_bag_item（存在则累加），并把明细与展示文案回给客户端。
    /// </summary>
    public GetRewardRet GetReward(GetRewardReq req)
    {
        GetRewardRet ret = new GetRewardRet();
        if (req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        switch (req.RewardType)
        {
            case 1: // 任务奖励：奖励内容与「能不能领」都由服务端判定，客户端不参与
                var taskData = LubanMgr.Instance.GetTaskDataById(req.TaskId);
                if (taskData == null)
                {
                    LogMsg.Info($"[Center]任务奖励发放失败：Luban 中找不到任务 {req.TaskId}");
                    ret.CmdCode = CmdCode.ReqParamError;
                    return ret;
                }

                RoleTaskProgress progress = _db.Queryable<RoleTaskProgress>()
                    .Where(v => v.RoleId == req.RoleId && v.TaskId == req.TaskId).First();
                // 只允许「已接取(1) / 条件达成(2)」的任务领奖；Locked 没资格、Claimed 已领过，直接拒绝以防重复发奖
                if (progress == null || progress.State < 1 || progress.State > 2)
                {
                    int state = progress?.State ?? -1;
                    LogMsg.Info($"[Center]任务 {req.TaskId} 当前不可领奖（state={state}）");
                    ret.CmdCode = CmdCode.ReqParamError;
                    return ret;
                }

                // 奖励内容与数量完全由 Luban taskData 配置驱动：
                for (int i = 0; i < taskData.DepotIds.Count; i++)
                {
                    AddToRewardMap(ret.RewardMap, taskData.DepotIds[i], GetRewardNum(taskData.DepotNums, i));
                }

                for (int i = 0; i < taskData.MaterialsId.Count; i++)
                {
                    AddToRewardMap(ret.RewardMap, taskData.MaterialsId[i], GetRewardNum(taskData.MaterialNums, i));
                }

                // 发奖即置为已领（3=Claimed），与客户端状态机保持一致
                progress.State = 3;
                progress.UpdateDate = DateTime.Now;
                _db.Updateable(progress).ExecuteCommand();
                break;
            default:
                LogMsg.Info($"[Center]暂不支持的奖励类型: {req.RewardType}");
                ret.CmdCode = CmdCode.ReqParamError;
                return ret;
        }

        if (ret.RewardMap.Count > 0)
        {
            GrantToBag(req.RoleId, ret.RewardMap);
            ret.RewardDesc = BuildRewardDesc(ret.RewardMap);
        }

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 取奖励数量
    /// </summary>
    private int GetRewardNum(IReadOnlyList<int> nums, int index)
    {
        if (nums == null || index < 0 || index >= nums.Count) return 1;
        return nums[index] > 0 ? nums[index] : 1;
    }

    /// <summary>累加奖励明细（同一 id 在配置里可能出现多次）</summary>
    private void AddToRewardMap(IDictionary<int, int> rewardMap, int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return;
        rewardMap.TryGetValue(itemId, out int current);
        rewardMap[itemId] = current + count;
    }

    /// <summary>把奖励明细写进角色背包：已有则累加，没有则新建</summary>
    private void GrantToBag(int roleId, IDictionary<int, int> rewardMap)
    {
        List<RoleBagInfo> existingItems = _db.Queryable<RoleBagInfo>().Where(v => v.RoleId == roleId).ToList();

        foreach (var kv in rewardMap)
        {
            int itemId = kv.Key;
            int addCount = kv.Value;
            RoleBagInfo exist = existingItems.Find(v => v.ItemId == itemId);
            if (exist != null)
            {
                exist.Count += addCount;
                exist.UpdateDate = DateTime.Now;
                _db.Updateable(exist).ExecuteCommand();
            }
            else
            {
                RoleBagInfo row = new RoleBagInfo
                {
                    RoleId = roleId,
                    ItemId = itemId,
                    ItemType = ItemCatalog.GetItemType(itemId),
                    ItemName = ItemCatalog.GetName(itemId),
                    Count = addCount,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now
                };
                // 驱动盘行必须按 Luban 模板初始化等级与词条，否则新手拿到盘时等级 / 属性全是 0
                InitDriverDiskRow(row);
                _db.Insertable(row).ExecuteCommand();
            }
        }
    }

    /// <summary>
    /// 用 Luban 的 driverDisk 模板初始化驱动盘行的等级 / 经验上限 / 词条；材料行或未配置编号保持默认值。
    /// 幂等：只按模板赋值，不清空 Count。
    /// </summary>
    private static void InitDriverDiskRow(RoleBagInfo row)
    {
        if (row == null) return;
        driverDisk template = LubanMgr.Instance.GetDriverDiskById(row.ItemId);
        if (template == null) return;

        row.Level = template.InitLevel;
        row.CurMaxFillValue = template.InitMaxFill;
        row.CurFillValue = 0f;
        row.BaseValue = template.BaseValue;
        row.AttackPercent = template.AttackPercent;
        row.DefensePercent = template.DefensePercent;
        row.HealthPercent = template.HealthPercent;
        row.BaoJiPercent = template.BaoJiPercent;
        row.IsEquipped = 0;
    }

    /// <summary>role_bag_item 的驱动盘行 → 协议 DriverDiskInfo（StartGame / 升级 / 背包拉取共用）</summary>
    private static DriverDiskInfo ToDriverDiskInfo(RoleBagInfo bagInfo)
    {
        return new DriverDiskInfo
        {
            Id = bagInfo.Id,
            DriverDiskId = bagInfo.ItemId,
            DriverDiskCount = bagInfo.Count,
            DriverDiskName = bagInfo.ItemName,
            BaseValue = bagInfo.BaseValue,
            AttackPer = bagInfo.AttackPercent,
            DefensePer = bagInfo.DefensePercent,
            HealthPer = bagInfo.HealthPercent,
            BaoJiPer = bagInfo.BaoJiPercent,
            Level = bagInfo.Level,
            CurMaxFillValue = bagInfo.CurMaxFillValue,
            CurFillValue = bagInfo.CurFillValue,
            IsEquipped = bagInfo.IsEquipped == 1
        };
    }

    /// <summary>role_bag_item 的材料行 → 协议 MaterialInfo</summary>
    private static MaterialInfo ToMaterialInfo(RoleBagInfo bagInfo)
    {
        return new MaterialInfo
        {
            MaterialId = bagInfo.ItemId,
            MaterialName = bagInfo.ItemName,
            MaterialCount = bagInfo.Count
        };
    }

    /// <summary>升级一个驱动盘时，单个升级材料被消耗的数量（金币 1 个抵 10 点，其余 1 个抵 1 点；与客户端 MaterialService 同源）</summary>
    private static int GetUpgradeCost(int materialId)
    {
        return materialId == ItemCatalog.Gold ? 10 : 1;
    }

    /// <summary>给驱动盘加经验并按需连升（等级 / 经验 / 词条都写回 row，由调用方落库）</summary>
    private static void AddExpToDriverDisk(RoleBagInfo row, int exp)
    {
        row.CurFillValue += exp;
        while (row.CurMaxFillValue > 0f && row.CurFillValue >= row.CurMaxFillValue)
        {
            row.Level++;
            row.CurFillValue -= row.CurMaxFillValue;
            // 下一级经验上限：在当前上限基础上 +200 ~ +500
            row.CurMaxFillValue = NextRandom((int)row.CurMaxFillValue + 200, (int)row.CurMaxFillValue + 500);
            UpgradeDriverDiskValue(row);
        }
    }

    /// <summary>升级时按驱动盘类型提升词条（随机区间与原客户端实现一致）</summary>
    private static void UpgradeDriverDiskValue(RoleBagInfo row)
    {
        driverDisk template = LubanMgr.Instance.GetDriverDiskById(row.ItemId);
        if (template == null) return;

        switch (template.DiskType)
        {
            case DriverDiskType.Attack:
                row.BaseValue += NextRandom(20, 100);
                row.AttackPercent += 10f;
                row.DefensePercent += 5f;
                row.HealthPercent += 3f;
                row.BaoJiPercent += 3f;
                break;
            case DriverDiskType.Defense:
                row.BaseValue += NextRandom(20, 100);
                row.DefensePercent += 10f;
                row.HealthPercent += 5f;
                row.BaoJiPercent += 2f;
                row.AttackPercent += 3f;
                break;
            case DriverDiskType.Health:
                row.BaseValue += NextRandom(20, 100);
                row.HealthPercent += 10f;
                row.DefensePercent += 5f;
                row.BaoJiPercent += 1f;
                row.AttackPercent += 2f;
                break;
            case DriverDiskType.BaoJi:
                row.BaseValue += NextRandom(5, 10);
                row.BaoJiPercent += 5f;
                row.AttackPercent += 3f;
                row.DefensePercent += 1f;
                row.HealthPercent += 1f;
                break;
            default:
                LogMsg.Info("[Center]驱动盘 " + row.ItemId + " 类型未知，升级词条已跳过");
                break;
        }
    }

    /// <summary>把奖励明细拼成客户端可直接展示的文本</summary>
    private static string BuildRewardDesc(IDictionary<int, int> rewardMap)
    {
        string des = "获得奖励\n";
        foreach (var kv in rewardMap)
        {
            des += ItemCatalog.GetName(kv.Key) + "×" + kv.Value + "\n";
        }

        return des;
    }

    #region 任务进度持久化

    /// <summary>
    /// 保存任务进度
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
    /// 拉取角色的全量任务
    /// </summary>
    public TaskProgressListRet LoadTaskProgress(TaskProgressReq req)
    {
        TaskProgressListRet ret = new TaskProgressListRet();
        if (req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        var taskDatas = LubanMgr.Instance.GetTaskDatas();
        if (taskDatas == null || taskDatas.Count == 0)
        {
            LogMsg.Info("LoadTaskProgress：Luban taskData 表为空，返回空任务列表");
            return ret;
        }

        // 该角色已有的进度记录
        var progressList = _db.Queryable<RoleTaskProgress>().Where(v => v.RoleId == req.RoleId).ToList();
        Dictionary<int, RoleTaskProgress> progressMap = new Dictionary<int, RoleTaskProgress>();
        foreach (RoleTaskProgress p in progressList)
        {
            progressMap[p.TaskId] = p;
        }

        foreach (var t in taskDatas.Select(kv => kv.Value))
        {
            progressMap.TryGetValue(t.TaskID, out RoleTaskProgress progress);

            TaskInfo info = new TaskInfo
            {
                TaskId = t.TaskID,
                TaskDesc = t.TaskDesc,
                TaskType = t.TaskType,
                TargetCount = t.TargetCount,
                Condition = t.Condition ?? string.Empty,
                // 1=InProgress 0=Locked（对应 TaskState 枚举）
                State = progress?.State ?? (t.IsUnlock ? 1 : 0),
                CurrentCount = progress?.CurrentCount ?? 0
            };
            info.DepotIds.Add(t.DepotIds);
            info.MaterialsId.Add(t.MaterialsId);
            ret.TaskList.Add(info);
        }

        return ret;
    }

    #endregion

    #region 背包（驱动盘升级 / 装备 / 全量拉取）

    /// <summary>
    /// 驱动盘升级
    /// </summary>
    public DriverDiskUpgradeRet UpgradeDriverDisk(DriverDiskUpgradeReq req)
    {
        DriverDiskUpgradeRet ret = new DriverDiskUpgradeRet();
        if (req.RoleId <= 0 || req.DepotId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "参数错误";
            return ret;
        }

        driverDisk template = LubanMgr.Instance.GetDriverDiskById(req.DepotId);
        if (template == null)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "驱动盘配置不存在";
            return ret;
        }

        RoleBagInfo disk = _db.Queryable<RoleBagInfo>()
            .Where(v => v.RoleId == req.RoleId && v.ItemId == req.DepotId
                                               && v.ItemType == ItemCatalog.DriverDiskType).First();
        if (disk == null)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "未拥有该驱动盘";
            return ret;
        }

        // 老数据 / 手工插入的行可能没初始化过，升级前先按模板补一次，避免经验上限为 0 算不出升级
        if (disk.Level <= 0 || disk.CurMaxFillValue <= 0f) InitDriverDiskRow(disk);

        List<RoleBagInfo> materials = _db.Queryable<RoleBagInfo>()
            .Where(v => v.RoleId == req.RoleId && v.ItemType == ItemCatalog.MaterialType).ToList();

        // 先整体校验，避免扣一半发现不够
        string shortageName = null;
        foreach (int materialId in template.UpgradeMaterials)
        {
            RoleBagInfo material = materials.Find(v => v.ItemId == materialId);
            if (material != null && material.Count >= GetUpgradeCost(materialId)) continue;
            if (shortageName == null) shortageName = ItemCatalog.GetName(materialId);
        }

        if (shortageName != null)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = shortageName + "不足";
            LogMsg.Info("[Center]驱动盘升级失败：roleId=" + req.RoleId + " depotId=" + req.DepotId + " " + ret.Tip);
            return ret;
        }

        // 扣除材料，并把剩余数量回给客户端
        foreach (int materialId in template.UpgradeMaterials)
        {
            RoleBagInfo material = materials.Find(v => v.ItemId == materialId);
            material.Count -= GetUpgradeCost(materialId);
            material.UpdateDate = DateTime.Now;
            _db.Updateable(material).ExecuteCommand();
            ret.MaterialMap[material.ItemId] = material.Count;
        }

        // 加经验 → 升级（含随机词条），然后落库
        int oldLevel = disk.Level;
        AddExpToDriverDisk(disk, AddExpPerUpgrade);
        disk.UpdateDate = DateTime.Now;
        _db.Updateable(disk).ExecuteCommand();

        ret.OldLevel = oldLevel;
        ret.DriverDisk = ToDriverDiskInfo(disk);
        ret.CmdCode = CmdCode.Succeed;
        LogMsg.Info("[Center]驱动盘升级成功：roleId=" + req.RoleId + " depotId=" + req.DepotId
                    + " Lv" + oldLevel + "->" + disk.Level);
        return ret;
    }

    /// <summary>
    /// 装备 / 卸下驱动盘
    /// </summary>
    public DriverDiskEquipRet SetDriverDiskEquipped(DriverDiskEquipReq req)
    {
        DriverDiskEquipRet ret = new DriverDiskEquipRet();
        if (req.RoleId <= 0 || req.DepotId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "参数错误";
            return ret;
        }

        ret.DepotId = req.DepotId;
        ret.Equipped = req.Equip;

        RoleBagInfo disk = _db.Queryable<RoleBagInfo>()
            .Where(v => v.RoleId == req.RoleId && v.ItemId == req.DepotId
                                               && v.ItemType == ItemCatalog.DriverDiskType).First();
        if (disk == null)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "未拥有该驱动盘";
            return ret;
        }

        if (req.Equip && disk.IsEquipped != 1)
        {
            int equippedCount = _db.Queryable<RoleBagInfo>()
                .Where(v => v.RoleId == req.RoleId && v.ItemType == ItemCatalog.DriverDiskType
                                                   && v.IsEquipped == 1).Count();
            if (equippedCount >= MaxEquipSlots)
            {
                ret.CmdCode = CmdCode.ReqParamError;
                ret.Tip = "装备槽已满";
                return ret;
            }
        }

        disk.IsEquipped = req.Equip ? 1 : 0;
        disk.UpdateDate = DateTime.Now;
        _db.Updateable(disk).ExecuteCommand();

        ret.CmdCode = CmdCode.Succeed;
        LogMsg.Info("[Center]驱动盘" + (req.Equip ? "装备" : "卸下") + "：roleId=" + req.RoleId + " depotId=" + req.DepotId);
        return ret;
    }

    /// <summary>
    /// 拉取角色全量背包
    /// </summary>
    public BagInfoRet GetBagInfo(BagInfoReq req)
    {
        BagInfoRet ret = new BagInfoRet();
        if (req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        List<RoleBagInfo> bagItems = _db.Queryable<RoleBagInfo>().Where(v => v.RoleId == req.RoleId).ToList();
        foreach (RoleBagInfo bagInfo in bagItems)
        {
            if (bagInfo.ItemType == ItemCatalog.DriverDiskType)
            {
                ret.DriverDiskMap[bagInfo.ItemId] = ToDriverDiskInfo(bagInfo);
            }
            else
            {
                ret.MaterialMap[bagInfo.ItemId] = ToMaterialInfo(bagInfo);
            }
        }

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    #endregion

    #region 好友系统

    /// <summary>每个玩家对最多保留的私聊消息条数</summary>
    private const int MaxFriendMsgPerPair = 100;

    /// <summary>
    /// 拉取好友列表（status==1 且与当前角色相关）。
    /// 好友昵称从 role 表查询；is_online 由 LoginServer 转发回包时补充（DB 层不知道在线状态）。
    /// </summary>
    public FriendListRet GetFriendList(FriendListReq req)
    {
        FriendListRet ret = new FriendListRet();
        if (req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        List<FriendRelation> relations = _db.Queryable<FriendRelation>()
            .Where(v => v.Status == 1 && (v.FromRoleId == req.RoleId || v.ToRoleId == req.RoleId))
            .ToList();

        LogMsg.Info($"[Center.DB]好友列表: roleId={req.RoleId} 好友关系={relations.Count} 条");

        foreach (FriendRelation rel in relations)
        {
            int friendId = rel.FromRoleId == req.RoleId ? rel.ToRoleId : rel.FromRoleId;
            RoleTable friend = _db.Queryable<RoleTable>().Where(v => v.Id == friendId).First();

            // 角色被删（手动改库 / 合服）留下的悬空关系必须跳过：
            // 下面直接读 friend.NickName，一旦为 null 就会抛异常，整个回包发不出去，客户端表现为「同步失败 / 一个好友都没有」
            if (friend == null)
            {
                LogMsg.Info($"[Center.DB]跳过悬空好友关系: relationId={rel.Id} roleId={req.RoleId} friendId={friendId}");
                continue;
            }

            ret.Friends.Add(new FriendInfo
            {
                RoleId = friendId,
                Nickname = friend.NickName,
                Avatar = AvatarCatalog.Normalize(friend.Avatar),
                IsOnline = false, // 由 LoginServer 转发时按在线状态覆盖
                // 未读数：离线期间对方发来的消息只落库不推送，只能靠这里回带给客户端点亮红点
                Unread = _db.Queryable<FriendMessage>()
                    .Where(v => v.SenderRoleId == friendId && v.ReceiverRoleId == req.RoleId && v.IsRead == 0)
                    .Count()
            });
        }

        // 待处理申请：别人向我发起、我还没处理（status==0 且 to == 我）。
        // 不能只靠 FriendApplyNtf 实时推送 —— 我不在线时推送直接丢掉且无法补，
        // 所以每次拉列表都把待处理申请一并带回，客户端打开面板就能看到。
        List<FriendRelation> applies = _db.Queryable<FriendRelation>()
            .Where(v => v.Status == 0 && v.ToRoleId == req.RoleId)
            .ToList();

        foreach (FriendRelation rel in applies)
        {
            RoleTable applicant = _db.Queryable<RoleTable>().Where(v => v.Id == rel.FromRoleId).First();
            ret.Applies.Add(new FriendInfo
            {
                RoleId = rel.FromRoleId,
                Nickname = applicant != null ? applicant.NickName : ("Player" + rel.FromRoleId),
                Avatar = applicant != null ? AvatarCatalog.Normalize(applicant.Avatar) : AvatarCatalog.Default,
                IsOnline = false // 申请项不展示在线状态，仅保持字段一致
            });
        }

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 添加好友：from 向 to 发起申请（status=0）。幂等保护与防重复：
    /// ① to 不存在 → 报错；② 已存在 from→to 或 to→from 的 status<=1 记录 → 已是好友 / 重复申请。
    /// </summary>
    public AddFriendRet AddFriend(AddFriendReq req)
    {
        AddFriendRet ret = new AddFriendRet();
        if (req.FromRoleId <= 0 || req.ToRoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "参数错误";
            return ret;
        }

        if (req.FromRoleId == req.ToRoleId)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "不能添加自己为好友";
            return ret;
        }

        RoleTable target = _db.Queryable<RoleTable>().Where(v => v.Id == req.ToRoleId).First();
        if (target == null)
        {
            ret.CmdCode = CmdCode.RoleNotExist;
            ret.Tip = "对方不存在";
            return ret;
        }

        // 已存在正向或反向的有效关系（待处理 / 已同意）
        bool exists = _db.Queryable<FriendRelation>()
            .Where(v => v.Status <= 1 &&
                        ((v.FromRoleId == req.FromRoleId && v.ToRoleId == req.ToRoleId) ||
                         (v.FromRoleId == req.ToRoleId && v.ToRoleId == req.FromRoleId)))
            .Any();
        if (exists)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "你们已经是好友或已有待处理申请";
            return ret;
        }

        _db.Insertable(new FriendRelation
        {
            FromRoleId = req.FromRoleId,
            ToRoleId = req.ToRoleId,
            Status = 0,
            CreateDate = DateTime.Now,
            UpdateDate = DateTime.Now
        }).ExecuteCommand();

        ret.CmdCode = CmdCode.Succeed;
        ret.FromRoleId = req.FromRoleId;
        ret.ToRoleId = req.ToRoleId;
        ret.ToNickname = target.NickName;
        // 申请者昵称（供推送通知展示）
        RoleTable from = _db.Queryable<RoleTable>().Where(v => v.Id == req.FromRoleId).First();
        ret.FromNickname = from != null ? from.NickName : ("Player" + req.FromRoleId);
        return ret;
    }

    /// <summary>
    /// 处理好友申请：同意 → 该条 status 置 1（成为好友）；拒绝 → 置 2。
    /// 返回处理结果，同时带上被处理申请的申请者昵称，供 LoginServer 推送通知。
    /// </summary>
    public HandleFriendRet HandleFriend(HandleFriendReq req)
    {
        HandleFriendRet ret = new HandleFriendRet();
        if (req.RoleId <= 0 || req.TargetRoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "参数错误";
            return ret;
        }

        // 申请方向固定为 target(申请者) → role(处理者)
        FriendRelation rel = _db.Queryable<FriendRelation>()
            .Where(v => v.FromRoleId == req.TargetRoleId && v.ToRoleId == req.RoleId && v.Status == 0)
            .First();
        if (rel == null)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            ret.Tip = "申请不存在或已处理";
            return ret;
        }

        rel.Status = req.Accept ? 1 : 2;
        rel.UpdateDate = DateTime.Now;
        _db.Updateable(rel).ExecuteCommand();

        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 发送私聊消息：落库，并把该玩家对的消息裁剪到最多 100 条。返回完整消息给发送者。
    /// </summary>
    public SendFriendMsgRet SendFriendMsg(SendFriendMsgReq req)
    {
        SendFriendMsgRet ret = new SendFriendMsgRet();
        if (req.SenderRoleId <= 0 || req.ReceiverRoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        if (string.IsNullOrEmpty(req.Content))
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        DateTime now = DateTime.Now;
        FriendMessage msg = new FriendMessage
        {
            SenderRoleId = req.SenderRoleId,
            ReceiverRoleId = req.ReceiverRoleId,
            Content = req.Content,
            SendTime = now,
            IsRead = 0 // 新消息对接收者恒为未读，接收者打开聊天时统一置 1
        };
        // 取自增主键必须用 ExecuteReturnIdentity()：
        // ExecuteCommand() 返回的是「受影响行数」（插入成功恒为 1），拿它当 id 会让每条消息的
        // Id 都变成 1 —— 客户端按 Id 去重（实时推送可能先到）时，第 2 条起会被全部当成重复丢掉，
        // 表现就是「服务端已落库，但发送方本地不生成气泡」（接收方的实时推送同理）。
        int id = _db.Insertable(msg).ExecuteReturnIdentity();
        if (id <= 0)
        {
            ret.CmdCode = CmdCode.ServerError;
            return ret;
        }

        msg.Id = id;
        TrimFriendMessages(req.SenderRoleId, req.ReceiverRoleId);

        ret.Message = ToFriendMsg(msg);
        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 拉取与某好友的最近聊天记录
    /// </summary>
    public FriendHistoryRet GetFriendHistory(FriendHistoryReq req)
    {
        FriendHistoryRet ret = new FriendHistoryRet();
        if (req.RoleId <= 0 || req.FriendRoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        int a = req.RoleId;
        int b = req.FriendRoleId;

        // 打开聊天 = 已读：把这个好友发给我的未读消息全部标记掉，
        MarkMsgRead(a, b);

        List<FriendMessage> list = _db.Queryable<FriendMessage>()
            .Where(v => (v.SenderRoleId == a && v.ReceiverRoleId == b) ||
                        (v.SenderRoleId == b && v.ReceiverRoleId == a))
            .OrderBy(v => v.Id, OrderByType.Desc).Take(MaxFriendMsgPerPair).ToList();

        // 倒序取出后反转为时间升序，方便客户端直接按顺序渲染
        list.Reverse();
        foreach (FriendMessage m in list) ret.Messages.Add(ToFriendMsg(m));
        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>
    /// 把 peer 发给 reader 的未读私聊全部置为已读。
    /// 拉历史时顺手调一次，聊天窗口里实时收到新消息时由客户端补一条 MarkFriendMsgReadReq。
    /// </summary>
    private void MarkMsgRead(int readerRoleId, int peerRoleId)
    {
        _db.Updateable<FriendMessage>()
            .SetColumns(v => v.IsRead == 1)
            .Where(v => v.SenderRoleId == peerRoleId && v.ReceiverRoleId == readerRoleId && v.IsRead == 0)
            .ExecuteCommand();
    }

    /// <summary>客户端显式标记已读（聊天窗口开着时收到新消息）</summary>
    public MarkFriendMsgReadRet MarkFriendMsgRead(MarkFriendMsgReadReq req)
    {
        MarkFriendMsgReadRet ret = new MarkFriendMsgReadRet();
        if (req.RoleId <= 0 || req.FriendRoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        MarkMsgRead(req.RoleId, req.FriendRoleId);
        ret.CmdCode = CmdCode.Succeed;
        return ret;
    }

    /// <summary>裁剪某玩家对的私聊消息到上限（保留最新 N 条）。</summary>
    private void TrimFriendMessages(int roleA, int roleB)
    {
        int count = _db.Queryable<FriendMessage>()
            .Where(v => (v.SenderRoleId == roleA && v.ReceiverRoleId == roleB) ||
                        (v.SenderRoleId == roleB && v.ReceiverRoleId == roleA))
            .Count();
        if (count <= MaxFriendMsgPerPair) return;

        // 找出该玩家对里最旧的 (count - 100) 条，删除
        List<FriendMessage> oldest = _db.Queryable<FriendMessage>()
            .Where(v => (v.SenderRoleId == roleA && v.ReceiverRoleId == roleB) ||
                        (v.SenderRoleId == roleB && v.ReceiverRoleId == roleA))
            .OrderBy(v => v.Id, OrderByType.Asc)
            .Take(count - MaxFriendMsgPerPair)
            .ToList();

        foreach (FriendMessage m in oldest)
        {
            _db.Deleteable<FriendMessage>().Where(v => v.Id == m.Id).ExecuteCommand();
        }
    }

    /// <summary>DB 消息 → 协议消息（时间格式化为 HH:mm:ss）</summary>
    private static FriendMsg ToFriendMsg(FriendMessage m)
    {
        return new FriendMsg
        {
            Id = m.Id,
            SenderRoleId = m.SenderRoleId,
            ReceiverRoleId = m.ReceiverRoleId,
            Content = m.Content,
            SendTime = m.SendTime.ToString("HH:mm:ss")
        };
    }

    #endregion
}