# ARPGDemo

仿《绝区零》风格的 ARPG 多人联机游戏。**客户端（Unity）+ 服务端（C#）+ Protobuf 契约放在同一个仓库里。**

## 为什么是一个仓库

协议是两端的共享契约。改一次协议要同时动四个地方：`.proto` → 服务端 handler → 客户端 handler → 重新编译的 `Network.dll`。这四者必须**同一次提交**，否则中间态根本跑不起来。拆成两个仓库只会让人肉维护 DLL 版本号，所以合并管理。

## 目录结构

```
.
├── Assets/                     Unity 客户端
│   ├── Scripts/HotUpdate/      自研热更代码（Network/ProtoHandler.cs 等）
│   └── Plugins/Net/            ← Network.dll 部署到这里（构建产物，但已入库）
├── Server/                     C# 服务端（.NET Framework 4.8）
│   ├── Network/                网络库 + 协议实体（两端共用的源码）
│   ├── CenterServer/           10110，唯一访问 MySQL
│   ├── LoginServer/            10120，房间/战斗/位置同步的实际承载者
│   ├── 03_GameServer/          10130，纯转发
│   └── 04_GateServer/          10140，纯转发
├── proto/                      Protobuf 契约源 ★ 唯一真相
├── Tools/deploy.ps1            一键：生成协议 → 编译 → 部署 DLL
└── ProjectSettings/ Packages/  Unity 工程配置
```

## 日常工作流

### 改协议（最常见，务必按这个顺序）

1. 改 `proto/proto/*.proto`
2. 跑部署脚本：
   ```powershell
   powershell -ExecutionPolicy Bypass -File Tools\deploy.ps1
   ```
3. 改服务端 `Server/LoginServer/Ctrl/LoginCtrl.cs`（或对应 Ctrl）
4. 改客户端 `Assets/Scripts/HotUpdate/Network/ProtoHandler.cs`
5. **一次性 commit 以上所有改动**（含自动生成的 `Server/Network/Proto/*.cs` 和 `Assets/Plugins/Net/*.dll`）

不要手改 `Server/Network/Proto/*.cs`，那是 protoc 生成的，下次跑脚本会被覆盖。

### 只改业务逻辑

直接改对应文件即可，不用跑脚本。

### 启动顺序

```
MySQL → CenterServer → LoginServer → 03_GameServer → 04_GateServer → Unity
```

进程间靠 `NetClient` 的 10 秒重连兜底，顺序反了不会崩，只是先刷一堆重连日志。

## 注意事项

- **首次运行**需要手动往 `game_server` 表插至少一条服务器记录（代码里的初始化是注释状态），否则登录拿不到服务器列表。
- 数据库连接串硬编码在 `Server/CenterServer/DB/DBMgr.cs`，换机器要改。
- `Assets/Plugins/Net/*.dll` 虽然来自服务端构建，但对 Unity 来说是外部依赖库，所以**入库**，保证 clone 下来能直接编译。
- 详细的架构分析与已知问题见 `Server/项目分析说明.md`。

## 被版本控制排除的目录

以下**不含任何 C# 代码**，为控制仓库体积已排除。clone 后若场景出现资源丢失（粉色材质），从原渠道重新导入即可：

| 目录 | 体积 | 说明 |
|------|------|------|
| `Assets/KawaiiCity/` | 1.1G | 第三方城市资源包 |
| `Assets/SharpUI/` | 13M | 第三方 UI 资源 |
| `Assets/StreamingAssets/yoo/` | — | YooAsset 打包产物，本地构建即可生成 |
| `Assets/DllBytes/` | — | HybridCLR 热更 DLL 产物 |
| `Library/`、`Builds/`、`HybridCLRData/`、`Bundles/`、`ServerData/` | ~11G | Unity 缓存与构建产物，引擎自动重建 |

反过来，这几个**含 C# 代码、被业务层引用，绝不能排除**：`Assets/YooAsset-3.0.3-beta`（697 个 .cs）、`Assets/UnityShop`（176 个 .cs）、`Assets/Resources/Third Parts/Vefects`（技能特效脚本）、`Assets/URP`。

完整规则见 `.gitignore`。
