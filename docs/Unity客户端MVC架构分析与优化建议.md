# UnderWordYoo — Unity 客户端架构分析与优化建议（MVC 视角）

> 分析范围：`Assets/Scripts`（AOT 层 + HotUpdate 层，共 88 个 .cs）
> 技术栈：Unity + HybridCLR 热更 + YooAsset 资源管理 + Protobuf 协议 + DOTween/URP/Cinemachine

---

## 0. 先说结论

项目**目录结构看起来是 MVC**（`Controller/`、`Data/`、`UI/`、`Manager/`），但实际职责是**混住的**：

| 层 | 名义 | 实际 |
|---|---|---|
| View (`UI/UIPanel`) | 只负责显示 | 发网络请求、校验输入、算数值、写全局状态、跳转面板流程 |
| Controller | 编排 | 只有 4 个，且 `GameController` 只是按键轮询壳子；一半模块根本没有 Controller |
| Model (`Data/`) | 数据与领域逻辑 | 近乎空壳；真正的运行时状态住在 `GameManager` 这个"全局可变状态桶"里 |

最严重的问题是**依赖方向是双向的**：View 直接读写 Model，Model 反过来 `OpenPanel` 驱动 View，`UIManager` 反向触发业务事件。事件总线（`EventCenter`）虽然写了，但实际上只注册了 5 个事件，UI 刷新几乎全靠"谁改数据谁去调 UI 的方法"。

### 架构上做得好的部分（建议保留）

- **`State_Machine` + `MonoManager` 统一派发 Update**：状态不继承 MonoBehaviour，靠 `MonoManager.AddUpdateListener` 集中驱动，避免了几十个 `Update()`。这是全项目最干净的设计，值得推广到其他模块。
- **AOT / HotUpdate 分离 + HybridCLR**：`IState_MachineOwner`、`IHotUpdateWindow` 等接口放 AOT、实现放热更，方向正确。
- **协议层独立**：`ProtoHandler`（592 行）集中封装网络请求，`ProtoHandler.OnRoomInfoChanged +=` 用 C# event 解耦，这部分比 UI 层干净得多。
- **`PoolMgr` 已存在**（153 行），但 UI 列表没用上。

---

## 1. View 层：承担了 Controller 和 Model 的职责

### 1.1 `LoginPanel` —— View 里塞了整套登录流程
`UI/UIPanel/LoginPanel.cs`（181 行）里同时做了：

- 发网络请求：`ProtoHandler.Instance.RequestServerList / RequestLogin / RequestLoginGameServer / RequestCreateRole`
- 输入校验：判空、判是否选服务器
- 写全局状态：`GameManager.Instance.accountId / roleId / serverId / curPlayerName`
- 流程编排：`ClosePanel<LoginPanel>` → `OpenPanel<TipPanel>` → `OpenPanel<StartPanel>`，甚至自动创建角色
- 错误码 → 文案映射

Login/Register/Chat/Depot/Improve/PlayerData 这些面板**全部没有对应的 Controller**。

### 1.2 `PlayerDataPanel` —— View 直接检索并修改 Model
```csharp
// PlayerDataPanel.cs:27
PlayerCtrl player = FindObjectOfType<PlayerCtrl>();
var newPlayerData = player.CalculatePlayerData(depotsData);
player.UpdatePlayerData(newPlayerData);   // View 改了 Model
```
同一个 `FindObjectOfType<PlayerCtrl>()` 在 `DepotItem.cs:102` 又来一次。

### 1.3 `PlayerDataPanel.OnEnable` —— 用 UI 文本当状态标记
```csharp
if (attackText.text == "？？？") { /* 首次初始化 */ }
```
**把状态存在 UI 的表现层里**。策划改一次占位文案，这段逻辑就永久失效。

### 1.4 `DepotPanel` —— 业务状态住在 View 里
- `public List<DriverDiskDataRuntime> equipedDepotList` —— **已装备列表（核心业务状态）是 View 的字段**
- `EquipDepot / UnequipDepot` 装备卸下逻辑全在 View
- `DepotItem.EquipOrUnequip()` 反过来调 `depotPanel.EquipDepot()` 再 `FindObjectOfType<PlayerDataPanel>()` 再 `UpdatePlayerData()` —— 一个列表项 View 驱动了整条全局流程

### 1.5 `DepotItem` —— 列表项里有 `Update()`
`UIItem/DepotItem.cs:42` 每个 item 一个 `Update()`，每帧 `Input.GetMouseButtonDown`。仓库里几十个驱动盘 = 几十个 Update 常驻。

`IsPointerOverImprovePanel()` 每次点击都 `new PointerEventData` + `new List<RaycastResult>` + LINQ `Any()`，纯 GC。

`OpenImproveOrEquipPanel()` 里 `GameObject.Find("improveOrEquipPanel")` 全局按名字查找，且只会关掉"找到的第一个"同名对象——多实例场景会关错。

---

## 2. Controller 层：职责不清 + 大面积缺失

### 2.1 输入处理散落在 6 个 Update 里，没有统一门禁

| 文件 | 按键 |
|---|---|
| `GameController.Update` | K / C / V / 5 / B |
| `GameManager.Update` | Esc |
| `TaskManager.Update` | Tab |
| `DialogueManager.Update` | Space / Esc |
| `PlayerCtrl.Update` | E / 6 / 1 / 2 / R / Shift / 鼠标 |
| `DepotItem.Update` | R / 鼠标左右键 |

后果：**互斥关系无法表达**。打开仓库时角色还能攻击、聊天框聚焦时还能翻滚（只能靠 `chatPanel.chatInput.isFocused` 打补丁）。

### 2.2 `LobbyController` —— Controller 硬编码 View 的内部结构
```csharp
panel          = GameObject.Find("Panel");
roomNameInput  = GameObject.Find("RoomName")?.GetComponent<TMP_InputField>();
creatBtn       = GameObject.Find("creatBtn")?.GetComponent<Button>();
// ...共 12 次
```
靠**字符串名字**拿引用，美术改一次节点名就崩。应改为 View 暴露序列化引用，Controller 只依赖 View 类型。

### 2.3 `TaskManager` —— 名为 Manager 实为 Controller
`Start()` 里 `OpenPanel` 后立刻 `ClosePanel`、手动 `localScale = Vector3.zero`；`CheckTaskFinish()` 里发奖励 + 开面板 + `GameManager.Instance.dialogueId++`。
`GiveTaskReward()` 还 `FindObjectOfType<GameManager>()` 只为借个协程宿主——而 TaskManager 自己就是 MonoBehaviour。

### 2.4 `DialogueController` —— 动画逻辑外置但依赖具体 UI 结构
直接持有左右两侧的 `RectTransform / CanvasGroup` 做 DOTween。View 的动画跑到了 Controller，却又绑死 UI 层级。

---

## 3. Model 层：近乎空壳，状态住在 Manager 里

### 3.1 `GameManager` 是"全局可变状态桶"
`playerBaseData / haveDepotList / curTasksData / materialNumDict / dialogueId / accountId / roleId / serverId / mainRoleInfo` 全挂在它身上，且它自己：
- `Update()` 里处理 Esc 输入
- `IsPointerOverSpecificUILayer()` 做 UI 射线检测（依赖 `UnityEngine.EventSystems`）
- `SendMes()` 里 `FindObjectOfType<ChatPanel>()`

**Model 依赖了 UI 和输入系统**，这是分层被击穿的典型症状。

### 3.2 `PlayerCtrl`（554 行）是 Controller + Model + View 三合一
- Model：`health / maxHealth / _playerData`
- Controller：`Update()` 读 Input、`HandleSkillSwitch / HandleEvadeSwitch`
- Model/领域：`CalculatePlayerData()`（纯数值计算，却被 View 直接调用，无法单测）
- View：`DoSpawnVFX / PlayLocalHitEffects / vignette DOTween / 音效`

而且血量有**三套并存**：`health`、`maxHealth`、`_playerData.maxHealthValue`，`UpdatePlayerData` 只同步其中两个，容易漂移。

### 3.3 `PlayerModel.cs` 名不副实
名字叫 Model，实际是"动画事件转发器"（`StartSkillHit / StopSkillHit / PlayFootSound`），本质是 View 层组件。命名误导会持续污染后续分工。

### 3.4 数值配置散落三处
`PlayerData` 字段默认值（`attackValue = 200` 硬编码）、`GameManager.playerBaseData`、`ScriptableObject`。改数值不知道该改哪。

---

## 4. 通信机制：4 套并行，方向混乱

1. 单例直调：`GameManager.Instance` / `UIManager.Instance` / `ProtoHandler.Instance`（最泛滥）
2. `EventCenter` 事件总线（只有 5 个事件：进度条加载 / 游戏开始 / 对话结束 / 光标出现 / 光标消失）
3. C# event：`ProtoHandler.OnRoomInfoChanged +=`（仅网络层用）
4. 直接 `Find` + 方法调用

**问题**：
- 事件总线几乎没用起来 → UI 刷新全靠 pull 式直调 → 双向强耦合
- `UIManager.TogglePanel()` 反向 `EventCenter.EventTrigger(光标出现/消失)`：View 管理器往业务方向发事件
- `GameManager`（Model 侧）直接 `UIManager.Instance.OpenPanel<ExitPanel>()`：Model 反向驱动 View

---

## 5. 明确的 Bug 和隐患（P0，建议优先修）

### 5.1 `BasePanel.Awake()` 被子类隐藏 ⚠️ 真实 Bug
```csharp
// BasePanel.cs
public virtual void Awake() { /* 绑定 CloseBtn */ }

// TaskPanel.cs:18
private new void Awake() { ... }   // ← new 关键字隐藏了基类实现
```
`TaskPanel` 的 `CloseBtn` 绑定**永远不会执行**。应改为 `protected override void Awake()` 并在基类里 `Awake` 声明为 `protected virtual`。

### 5.2 `BasePanel` 反向依赖具体子类
```csharp
// BasePanel.cs:23 —— 基类里写死了子类
if (UIManager.Instance.GetPanel<ImprovePanel>() != null && ...) 
    UIManager.Instance.ClosePanel<ImprovePanel>();
UIManager.Instance.ClosePanel<PlayerDataPanel>();
```
基类依赖子类，违反依赖倒置。每个面板的关闭逻辑都不同却硬编码在基类。应改成 `protected virtual void OnCloseClicked()` 由子类重写。

### 5.3 `ResMgr.instanceCache` 只增不减（内存泄漏）
`LoadAndInstantiateAsync` 把每个实例塞进 `instanceCache`，但 `DepotPanel.UpdateDepotInfo` / `TaskPanel.ClearAllTaskItem` 清理列表时用的是 `Destroy(child.gameObject()`，**从不调 `ReleaseInstance`** → List 永久持有已销毁对象的引用，无法 GC，且 `ReleaseAll()` 时会重复 Destroy。

### 5.4 `UIManager` 面板资源只加不卸
`panelAssetHandles` 只在 `DestroyPanel()` 释放，`ClosePanel()` 只 Hide + 换父节点，`ClearAllPanel()` 也只是 Hide。

### 5.5 `PlayerCtrl.OnDestroy` 移除从未注册的监听
```csharp
EventCenter.Instance.RemoveEventListener(GameEvent.游戏开始, Init);  // 但从未 AddEventListener
```
死代码，说明"注册/注销不成对"是常态。`Start()` 里 `ProtoHandler.StartPositionSync(...)` 也没有对应停止。

### 5.6 `EventCenter` 无自动清理
`_eventDict` 即使 `actions` 为 null 也不 Remove；`Clear()` 定义了但全项目无调用点。场景切换时监听器会残留。

### 5.7 `GameEvent` 枚举用中文命名
`光标出现 / 游戏开始 / 进度条加载`。能编译，但不利于搜索与协作；且热更 DLL 新增枚举值时需注意 AOT 侧引用。

---

## 6. 性能问题

| 问题 | 位置 | 影响 |
|---|---|---|
| `FindObjectOfType` / `GameObject.Find` 满天飞 | `LobbyController` 12 次、`PlayerCtrl` 6 次、`DepotItem`、`ChatPanel`、`GameManager.SendMes` | 全场景遍历；靠字符串，改名即崩 |
| **每帧全场景遍历** | `PlayerCtrl.cs:164` `chatPanel ??= FindObjectOfType<ChatPanel>()` —— 找不到时永远为 null，于是每帧 Find | 聊天面板未打开时每帧一次全场景扫描 |
| N 个 UIItem 各自 `Update()` | `DepotItem.cs:42` | 几十个 Update 常驻 + 每帧鼠标检测 |
| `Resources.Load` 与 YooAsset 混用 | `DepotPanel` / `DepotItem` / `ImprovePanel` / `PlayerRoomItem` 的图标加载 | Resources **无法热更**，且每次调用都重新查找无缓存 |
| 每次点击的 GC 分配 | `IsPointerOverImprovePanel()`、`GameManager.IsPointerOverSpecificUILayer()` | `new PointerEventData` + `new List` + LINQ |
| Destroy/Instantiate 代替对象池 | `DepotPanel.UpdateDepotInfo`、`TaskPanel.ClearAllTaskItem` + `CreateTaskItem` | 每次开关面板全量重建；`PoolMgr` 已存在却没用 |
| 攻击时 `FindGameObjectsWithTag("enemy")` | `PlayerAttackState.cs:369`、`EXAttackState.cs:126` | 分配数组 + 全场景扫描，应改 `OverlapSphere` 或缓存 |
| 6 处 `Update()` 各自轮询 Input | 见 2.1 | 无统一门禁 |

---

## 7. 热更架构（AOT / HotUpdate）补充

- ✅ AOT 放 `State_Machine / State_Base / MonoManager / IHotUpdateWindow`，热更侧实现 —— 方向正确
- ⚠️ `EventCenter` / `GameEvent` 放在 HotUpdate，AOT 侧无法使用事件总线。建议把 `EventCenter` 下沉到 AOT，或至少保证 AOT 永不依赖 HotUpdate
- ⚠️ `RemotePlayerManager.cs` 一个文件里放了 3 个 public class（`RemotePlayerManager` / `RemotePlayer` / `RemoteEnemy`），建议拆文件

---

## 8. 优化路线（按优先级）

### P0 — 正确性与泄漏（1～2 天）
1. `TaskPanel` 的 `private new void Awake()` → `protected override void Awake()`；全局排查其他 `new` 隐藏
2. `BasePanel` 抽 `protected virtual void OnCloseClicked()`，删掉对 `ImprovePanel` / `PlayerDataPanel` 的硬编码
3. `ResMgr` 的 `instanceCache` 改用弱引用或由调用方显式 `ReleaseInstance`；`DepotPanel` / `TaskPanel` 清理时同步释放
4. 图标加载从 `Resources.Load` 统一走 `ResMgr`（YooAsset）+ 缓存

### P1 — 结构重构（1～2 周）
5. **抽 Controller**：为 Login / Register / Chat / Depot / Improve / PlayerData 各建一个 Controller；Panel 只留 `Render(model)` 和 `event Action<...> OnXxxClicked`
6. **Model 纯化**：
   - 从 `GameManager` 拆出 `PlayerDataService / DepotService / TaskService / RoomService`，尽量不继承 MonoBehaviour
   - `CalculatePlayerData()` 移出 `PlayerCtrl`，进 `PlayerDataService`（纯 C#，可单测）
   - `GameManager` 删掉 `Update()` 里的 Esc 输入和 `IsPointerOverSpecificUILayer`
   - `PlayerCtrl` 拆成 `PlayerCtrl`(Controller) + `PlayerState`(Model) + `PlayerView`
7. **统一通信**：新增 `GameEvent`（玩家数据变更 / 仓库变更 / 任务变更 / 房间变更），Model 变更后广播，View 在 `OnEnable` 订阅、`OnDisable` 取消
8. **统一输入**：建 `InputManager`，所有按键集中注册 + 全局 `InputLock` 栈（开面板时压栈锁住角色输入）
9. **UIManager 拆分**：`UIManager`（层级/栈/生命周期）+ `UIAssetLoader`（YooAsset 加载释放）；面板路径用配置或 `[PanelPath]` attribute 替代 `GetPanelKey` 拼字符串

### P2 — 性能（穿插做）
10. 消除 UIItem 的 `Update()`：改 `IPointerClickHandler` / `EventTrigger` 或由父面板统一派发
11. UI 列表走 `PoolMgr`，`DepotPanel` / `TaskPanel` 的列表复用 item
12. `RaycastAll` 的 `List<RaycastResult>` 复用为静态成员；能用 `IsPointerOverGameObject()` 就别 RaycastAll
13. 敌人查找改 `OverlapSphere` + 缓存列表
14. `GameObject.Find` / `FindObjectOfType` 全部替换为序列化引用或 ServiceLocator 注册

### 关于是否引入 MVVM / 数据绑定
对这个项目规模，**不建议一上来上完整框架**。轻量做法：写一个 `BindableProperty<T>`（值变更自动回调），View 持有并订阅；重型可选 R3(UniRx) 或 Unity-MVVM。
当前 `EventCenter` + `BindableProperty` 足够，先把方向掰成单向，比换框架收益大得多。

---

## 9. 一句话总结

**目录是 MVC，依赖不是。** 优先把"View 直连 Model/网络"和"Model 反向 OpenPanel"这两条红色链路打断，把 `GameManager` 这个状态桶拆成领域 Service，再把输入和 UI 列表的 Update 收敛掉 —— 这套代码的骨架（状态机、热更分层、协议层）其实不差，缺的只是层与层之间的边界。
