# ARPGDemo_Server 使用与解析文档

## 一、项目概述

这是一个 **ARPG（动作角色扮演游戏）服务端**，基于 **C# .NET Framework 4.8** 开发，采用 **分布式多服务器架构**。项目使用 Visual Studio 2022 构建，通过 TCP + Protobuf 协议进行网络通信，使用 MySQL 作为数据库。

### 技术栈

| 技术 | 说明 |
|------|------|
| C# .NET Framework 4.8 | 开发语言和运行时 |
| Google.Protobuf | 网络消息序列化/反序列化 |
| MySQL 9.2 + SqlSugar ORM | 数据库存储与ORM |
| Luban | 游戏配置表（Excel→二进制）导出工具 |
| GZip | 网络数据压缩 |
| CRC16 | 数据传输完整性校验 |

---

## 二、整体架构

### 2.1 服务器拓扑结构

项目采用**链式代理转发**架构，共有 4 个核心服务器 + 1 个公共网络库：

```
                        ┌──────────────┐
                        │  MySQL数据库  │
                        │  (game库)     │
                        └──────┬───────┘
                               │
                        ┌──────┴───────┐
                        │ CenterServer │  ← 唯一有数据库访问权限的服务器
                        │  端口:10110  │
                        └──────┬───────┘
                               │
                    ┌──────────┼──────────┐
                    │                     │
              ┌─────┴─────┐        ┌──────┴──────┐
              │LoginServer│        │ 03_GameServer│
              │ 端口:10120│        │  端口:10130  │
              └─────┬─────┘        └──────┬──────┘
                    │                     │
                    │              ┌──────┴──────┐
                    │              │ 04_GateServer│
                    │              │  端口:10140  │
                    │              └──────┬──────┘
                    │                     │
                    └──────────┬──────────┘
                               │
                        ┌──────┴───────┐
                        │  Unity客户端  │
                        └──────────────┘
```

### 2.2 各服务器职责

| 服务器 | 端口 | 角色 | 职责 |
|--------|------|------|------|
| **CenterServer** | 10110 | 中心/数据库服务器 | 处理所有业务逻辑，操作数据库（注册、登录、创角等）|
| **LoginServer** | 10120 | 登录服务器 | 中转Unity客户端的登录/注册请求到CenterServer，做基础数据校验 |
| **03_GameServer** | 10130 | 游戏逻辑服务器 | 中转网关的创角/进游戏请求到CenterServer |
| **04_GateServer** | 10140 | 网关服务器 | Unity客户端的直接入口，转发请求到GameServer |

### 2.3 数据流转路径

两条主要的请求链路：

**登录流程：**
```
Unity客户端 → LoginServer(10120) → CenterServer(10110) → MySQL
                                                    ↓
Unity客户端 ← LoginServer(10120) ← CenterServer(10110)
```

**游戏流程（创角/进入游戏）：**
```
Unity客户端 → GateServer(10140) → GameServer(10130) → CenterServer(10110) → MySQL
                                                                    ↓
Unity客户端 ← GateServer(10140) ← GameServer(10130) ← CenterServer(10110)
```

---

## 三、项目结构详解

```
ARPGDemo_Server/
├── ARPGDemo_Server.sln              # VS解决方案文件
│
├── Network/                         # 【公共网络库】所有服务器共享
│   ├── Socket/
│   │   ├── NetServer.cs             # TCP服务端（监听端口、接受连接）
│   │   ├── NetClient.cs             # TCP客户端（连接其他服务器，支持断线重连）
│   │   └── com/
│   │       ├── NetDefine.cs         # 网络常量（IP、端口、命令码）
│   │       ├── ServerBase.cs        # 基类（接收/发送/粘包处理/命令分发）
│   │       ├── Session.cs           # 会话对象（每个连接一个Session）
│   │       ├── SessionMgr.cs        # 会话管理器（管理所有Session）
│   │       ├── NetUtils.cs          # 网络工具（GZip压缩、CRC16校验、封包拆包）
│   │       └── IContainer.cs        # 命令处理器接口
│   ├── Proto/
│   │   ├── RequestEntity.cs         # Protobuf请求消息定义（自动生成）
│   │   └── ResultEntity.cs          # Protobuf响应消息定义（自动生成）
│   ├── Common/
│   │   ├── Singleton.cs             # 泛型单例基类
│   │   ├── LogMsg.cs               # 日志输出工具
│   │   └── DataUtils.cs            # 数据验证工具
│   └── LubanManager/               # Luban配置表加载器
│       ├── LubanMgr.cs              # 配置表管理器
│       ├── Codes/                   # Luban自动生成的配置类
│       └── Tb/tbskillinfo.bytes     # 技能配置表二进制数据
│
├── CenterServer/                    # 中心服务器（端口10110）
│   ├── Program.cs                   # 入口：启动TCP服务+初始化数据库+Luban
│   ├── Ctrl/Center_LoginCtrl.cs     # 控制器：处理6种业务命令
│   └── DB/
│       ├── DBMgr.cs                 # 数据库管理器（初始化连接+建库建表）
│       ├── Modle/LoginModle.cs      # 业务逻辑层（注册/登录/选服/创角/进游戏）
│       └── Table/                   # 数据库表实体
│           ├── AccountTable.cs      # 账户表 account
│           ├── GameServerTable.cs   # 服务器列表表 game_server
│           └── RoleTable.cs         # 角色表 role
│
├── LoginServer/                     # 登录服务器（端口10120）
│   ├── Program.cs                   # 入口：作为客户端连CenterServer+作为服务端监听
│   └── Ctrl/LoginCtrl.cs           # 控制器：接收Unity请求→转发Center→回传Unity
│
├── 03_GameServer/                   # 游戏逻辑服务器（端口10130）
│   ├── Program.cs                   # 入口：作为客户端连CenterServer+作为服务端监听
│   └── Ctrl/Game_LoginCtrl.cs      # 控制器：接收Gate请求→转发Center→回传Gate
│
├── 04_GateServer/                   # 网关服务器（端口10140）
│   ├── Program.cs                   # 入口：作为客户端连GameServer+作为服务端监听
│   └── Ctrl/Gate_LoginCtrl.cs      # 控制器：接收Unity请求→转发Game→回传Unity
│
└── Game_Server/                     # GM管理服务器（空壳，未实现）
    └── Program.cs                   # 空Main方法
```

---

## 四、网络层实现原理

### 4.1 消息格式

每条网络消息的二进制结构如下：

```
┌─────────────┬──────────────┬──────────────┬──────────────┬─────────────────┐
│ 消息头(2B)  │ 压缩标志(1B) │ CRC16校验(2B)│  消息体(N B) │                 │
│ 总长度=5+N  │ true/false   │ 数据完整性校验│ Protobuf数据  │                 │
└─────────────┴──────────────┴──────────────┴──────────────┴─────────────────┘
```

- **消息头**：2字节无符号整数，值为 `1+2+N`（压缩标志+CRC+消息体 一共的长度）
- **压缩标志**：1字节bool值，消息体 > 200字节时启用GZip压缩
- **CRC16**：2字节校验码，确保数据在传输中未被篡改
- **消息体**：Google Protobuf 序列化后的二进制数据

### 4.2 TCP粘包处理

由于TCP是流式协议，可能出现粘包（多条消息连在一起）或半包（一条消息分多次到达）。处理方式：

```
ServerBase.OnReceiveCB():
  1. 从缓冲区头部读取2字节 → 得到消息长度 msgLen
  2. 如果当前接收到的数据 ≥ msgLen+2（有完整的消息体+消息头）
     → 截取一条完整消息进行解析
  3. 解析完成后，将剩余数据移到缓冲区头部
  4. 循环检查是否还有完整消息（处理粘包）
  5. 如果剩余数据 < msgLen+2 → break等待下次数据到达（处理半包）
```

### 4.3 消息分发机制

采用**命令码路由**模式：

1. 每个服务器启动时，通过 `RegistCommand(命令码, 控制器对象)` 注册命令处理函数
2. 收到消息后，解析Protobuf，根据 `BasePackage.ProtoCode` 在 `_cmdDic` 字典中查找对应的 `IContainer`
3. 根据当前角色（服务端/客户端），调用 `OnServerCommand()` 或 `OnClientCommand()`

### 4.4 Session（会话）机制

- 每个TCP连接对应一个 `Session` 对象
- `SessionMgr` 使用线程安全的 `Interlocked.Increment` 生成唯一 `SessionId`
- **关键作用**：中转服务器通过 `basePackage.UnitySessionId` 和 `basePackage.GateSessionId` 追踪原始客户端

SessionId 的生命周期：
```
Unity客户端连接到LoginServer  → 创建Session，记录SessionId（如 SessionId=5）
LoginServer转发请求到CenterServer → 在basePackage中设置 UnitySessionId=5
CenterServer处理完返回给LoginServer → 携带 UnitySessionId=5
LoginServer通过SessionId=5找到对应Session → 把结果发送回正确的Unity客户端
```

---

## 五、数据库设计

### 5.1 连接信息

```
ConnectionString: Server=localhost;Port=3306;DataBase=game;User=root;Password=123456;
```

### 5.2 表结构

#### account（账户表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (自增主键) | 账户ID |
| State | byte (默认1) | 状态 (1:正常 其他:禁用) |
| UserName | varchar(30) | 用户名 |
| PhoneNum | varchar(15) | 手机号 |
| Password | varchar(30) | 密码（明文存储，生产环境需加密） |
| LastLoginServerId | int | 最后登录的服务器ID |
| CreateDate | datetime | 创建时间 |
| UpdateDate | datetime | 更新时间 |

#### game_server（服务器列表表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (自增主键) | 服务器ID |
| State | byte | 状态 |
| ServerName | varchar(30) | 服务器名称 |
| RunState | byte | 运行状态 (1:爆满 2:拥挤 3:正常) |
| IsNew | byte | 是否新服 (1:是 0:否) |
| IPHost | varchar(30) | 服务器IP |
| Port | int | 服务器端口号 |
| CreateDate | datetime | 创建时间 |
| UpdateDate | datetime | 更新时间 |

#### role（角色表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (自增主键) | 角色ID |
| State | byte | 状态 |
| AccountId | int | 所属账户ID |
| Money | int | 金币 |
| NickName | varchar | 昵称 |
| JobID | int | 职业ID |
| Level | int | 等级 |
| Exp | int | 经验值 |
| SkillUpPoint | int | 技能升级点 |
| MaxHP / CurrHP | int | 最大/当前生命值 |
| MaxMP / CurrMp | int | 最大/当前法力值 |
| Atk / Def / Crit / Dodeg / Hit / Penet | int | 攻击/防御/暴击/闪避/命中/穿透 |
| Pos | varchar(50) | 角色位置 |
| CameraOffset | varchar(50) | 摄像机偏移 |
| MapId | int | 当前地图ID |
| ServerId | int | 所属服务器ID |
| CreateDate / UpdateDate | datetime | 创建/更新时间 |

---

## 六、业务命令码与流程

### 6.1 命令码定义

| 命令码 | 常量名 | 功能 |
|--------|--------|------|
| 11010 | CMD_RegistCode | 注册账号 |
| 11020 | CMD_LoginCode | 登录账号 |
| 11030 | CMD_GetServerListCode | 获取服务器列表 |
| 11040 | CMD_LoginGameServerCode | 登录游戏服务器（选服进入） |
| 11050 | CMD_CreateRoleCode | 创建角色 |
| 11060 | CMD_StartGameCode | 开始游戏（进入世界） |
| 10001 | CMD_ErrCode | 错误响应 |

### 6.2 注册流程（CMD_RegistCode）

```
Unity                             LoginServer                       CenterServer                        MySQL
  │                                    │                                  │                                │
  │──RegistReq(用户名,手机,密码)──────→│                                  │                                │
  │                                    │──校验用户名/手机/密码格式合法──→│                                │
  │                                    │                                  │──查询用户名是否已存在──────────→│
  │                                    │                                  │←──查询结果──────────────────────│
  │                                    │                                  │──若不存在，插入新账号──────────→│
  │                                    │←──RegistRet(成功/失败码)────────│                                │
  │←──RegistRet(成功/失败码)──────────│                                  │                                │
```

登录服务器做的基础校验：
- 用户名格式是否合法 (`DataUtils.IsValidUserName`)
- 手机号格式是否合法 (`DataUtils.IsValidMobile`)
- 密码长度是否在 4~16 位之间

### 6.3 登录流程（CMD_LoginCode）

```
Unity → LoginServer → CenterServer → MySQL查询账号
                     ← CenterServer ← 验证密码 + 查询最后登录的服务器信息
Unity ← LoginServer ← 返回 loginRet（含账户ID + 上次登录的服务器信息）
```

### 6.4 获取服务器列表（CMD_GetServerListCode）

```
Unity → LoginServer → CenterServer → MySQL查询 game_server 表
                     ← CenterServer ← 返回服务器列表
Unity ← LoginServer ← GetServerListRet
```

### 6.5 登录游戏服务器（CMD_LoginGameServerCode）

```
Unity → GateServer → GameServer → CenterServer → MySQL（更新LastLoginServerId + 查询角色）
                                   ← CenterServer ← 返回角色信息（已创建则返回角色数据）
Unity ← GateServer ← GameServer ← loginGameServerRet
```

### 6.6 创建角色（CMD_CreateRoleCode）

```
Unity → GateServer → GameServer → CenterServer → MySQL（检查昵称重复 + 插入角色数据）
                                   ← CenterServer ← 返回 CreateRoleRet（角色ID/昵称/职业/等级）
Unity ← GateServer ← GameServer ←
```

### 6.7 开始游戏（CMD_StartGameCode）

```
Unity → GateServer → GameServer → CenterServer → MySQL（查询角色完整数据）
                                   ← CenterServer ← StartGameRet（完整角色属性）
Unity ← GateServer ← GameServer ←
```

---

## 七、服务器启动与运行

### 7.1 前置条件

1. **安装 MySQL**：确保本地 MySQL 服务已启动，端口3306，root密码123456
2. **Visual Studio 2022**：安装 .NET Framework 4.8 开发工具
3. **NuGet 包还原**：打开解决方案后自动还原依赖包

### 7.2 启动顺序

**启动顺序很重要**：必须先启动 CenterServer，因为其他服务器启动时会尝试连接它。

```
第1步：启动 CenterServer    → 监听 10110，初始化数据库和Luban配置表
第2步：启动 LoginServer     → 连接 CenterServer(10110)，监听 10120
第3步：启动 03_GameServer   → 连接 CenterServer(10110)，监听 10130
第4步：启动 04_GateServer   → 连接 GameServer(10130)，监听 10140
```

启动方式：
- **方式A**：在 VS 中右键每个项目 → 调试 → 启动新实例（可以同时启动多个）
- **方式B**：在 VS 中设置解决方案为多项目启动（推荐）
- **方式C**：编译后手动运行各项目的 .exe 文件

### 7.3 多项目启动配置（推荐）

1. 在 VS 中右键解决方案 → 属性
2. 选择"启动项目" → "多个启动项目"
3. 将 CenterServer、LoginServer、03_GameServer、04_GateServer 都设为"启动"
4. 按 F5 一键启动所有服务器

### 7.4 验证服务器是否正常运行

启动成功后，各服务器控制台应输出：
```
CenterServer: "服务器开启成功：127.0.0.1:10110"
LoginServer:  "连接服务端成功:127.0.0.1:10110" + "服务器开启成功：127.0.0.1:10120"
GameServer:   "连接服务端成功:127.0.0.1:10110" + "服务器开启成功：127.0.0.1:10130"
GateServer:   "连接服务端成功:127.0.0.1:10130" + "服务器开启成功：127.0.0.1:10140"
```

### 7.5 初始化服务器列表数据

`CenterServer/DB/DBMgr.cs` 中有一段被注释掉的代码，用于批量创建20个服务器信息。首次运行时，需要取消注释来初始化数据：

```csharp
// 在 DBMgr.cs 的 InitDB() 方法中，取消以下注释：
for (int i = 0; i < 20; i++)
{
    GameServerTable gameServerTable = new GameServerTable()
    {
        ServerName = (i+1) + "区 镖人" + (i+1) + "队",
        RunState = 1,
        IsNew = 1,
        IPHost = NetDefine.IPHost,
        Port = NetDefine.GateServerPort,
        CreateDate = DateTime.Now,
        UpdateDate = DateTime.Now,
    };
    db.Insertable(gameServerTable).ExecuteCommand();
}
```

### 7.6 注意事项

1. **硬编码路径问题**：`Network/LubanManager/LubanMgr.cs` 中加载配置表的路径是写死的：
   ```csharp
   $"F:\\技能实训班\\游戏技能实践班2026\\ARPGDemo_Server\\Network\\LubanManager\\Tb\\{file}.bytes"
   ```
   如果你的项目不在 `F:\技能实训班\游戏技能实践班2026\` 下，需要修改为实际路径，或者改为相对路径。

2. **MySQL 密码**：默认 root/123456，如果不同需要修改 `DBMgr.cs` 中的连接字符串。

3. **端口占用**：确保 10110、10120、10130、10140 端口没有被其他程序占用。

4. **防火墙**：所有服务器绑定在 `127.0.0.1`（本地），不涉及外部网络，无需配置防火墙。

---

## 八、核心设计模式

### 8.1 代理/转发模式

LoginServer、GameServer、GateServer 都是**透明代理**——它们收到请求后不做业务处理，直接转发给 CenterServer，拿到结果后再原路返回。这样设计的优点：
- **安全性**：只有 CenterServer 能访问数据库，中间服务器只是数据通道
- **扩展性**：可以水平扩展中间层服务器来分散连接压力
- **解耦**：每个服务器职责单一

### 8.2 Session 追踪机制

在转发链中追踪原始客户端是核心难点，本项目的解决方案：

```
Session.cs HandleCommand():
  if (_Client类型是LoginServer或GateServer):
      basePackage.UnitySessionId = SessionId;    // 记录Unity客户端的SessionId
  if (_Client类型是GameServer):
      basePackage.GateSessionId = SessionId;      // 记录GateServer的SessionId
```

回传时：
```
LoginServer:  SessionMgr.GetSession(basePackage.UnitySessionId) → 找到Unity的Session → 发送
GameServer:   SessionMgr.GetSession(basePackage.GateSessionId)  → 找到Gate的Session  → 发送
GateServer:   SessionMgr.GetSession(basePackage.UnitySessionId) → 找到Unity的Session → 发送
```

### 8.3 命令模式

所有业务处理通过 `IContainer` 接口统一：
```
IContainer
  ├─ OnInit()          : 初始化
  ├─ OnServerCommand() : 作为服务端收到客户端请求时调用
  └─ OnClientCommand() : 作为客户端收到服务端响应时调用
```

服务器通过 `RegistCommand(命令码, IContainer实现)` 注册处理函数，实现了命令码到处理逻辑的解耦。

### 8.4 断线重连

`NetClient` 内置自动重连机制：
- 断线后 3 秒开始第一次尝试重连
- 之后每 10 秒尝试一次
- 通过 `_isNeedReconn` 标志控制是否启用

---

## 九、常见问题排查

| 问题 | 可能原因 | 解决方式 |
|------|----------|----------|
| CenterServer启动后无响应 | MySQL未启动或密码错误 | 检查MySQL服务，确认连接字符串 |
| LoginServer报"Disconnect" | CenterServer未先启动 | 按顺序先启动CenterServer |
| Luban配置加载失败 | 硬编码路径不存在 | 修改LubanMgr.cs中的文件路径 |
| 注册提示"账号已存在" | 之前注册过相同用户名 | 清空数据库account表后重试 |
| 获取服务器列表为空 | game_server表无数据 | 取消DBMgr.cs中的注释代码重新运行 |
| 端口冲突 | 其他程序占用了端口 | 修改NetDefine.cs中的端口号 |

---

## 十、后续扩展方向

1. **GM_Server（Game_Server项目）**：目前是空壳，可实现GM管理后台功能
2. **战斗系统**：当前只有登录/选服/创角/进游戏流程，后续需要在GameServer中添加战斗逻辑
3. **多服架构**：当前 game_server 表设计了多服字段，可以扩展为真正的多服架构
4. **密码加密**：当前密码明文存储，生产环境应使用 BCrypt 或 PBKDF2 加密
5. **消息加密**：当前TCP传输未加密，可加TLS或自定义加密层
