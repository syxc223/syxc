# AGENTS.md — 毕设v.0.1 项目上下文 / Project Context for AI Agents

> **本文档用途**：供外部 AI 工具（Codex / Cursor / Claude Code / 网页 LLM 等）只读本文即可建立对项目的完整认知，无需扫描全仓库。
> **最后核对**：2026-09-11，基于对当前代码 / 场景 / 配置的实际核验（GUID 检索、脚本通读），非转述旧文档。
> 项目内 `CODELY.md` 是 Codely CLI 的工作记忆，其中部分 TODO 已过时（见 §15）；`玩法建议.md`（2026-08-24）中提及的 `Mechanism` / `SharedAltar` / `LevelSpawnManager` 等脚本已不存在。**一切以代码为准，其次以本文为准。**
> 本文档为只读上下文，不代表授权修改项目。

---

## 0. 快速摘要 | TL;DR

- **Unity 2022.3.62f3c1（团结引擎 Tuanjie）+ URP 14.0.12** 的 HD2D 双人在线合作解谜游戏（毕业设计，Windows Standalone）。
- 联机：**Netcode for GameObjects 1.12.2**（UDP 7777 直连，服务器权威模型）；账号 / 房间匹配走 HTTP 后端 `server/`（Node.js + SQLite + JWT，部署于 `http://8.130.65.176:3000`），游戏数据不经过云服务器。
- 当前玩法闭环：登录 → 大厅建房 / 加房 → NPC 殿对话选任务 → Level1 → 双出口通关 → 返回 NPC 殿。压力板 / 门脚本已就绪，但 **Level1 场景尚未布置任何压力板 / 门**。
- **有两棵脚本树，勿混淆**：`Assets/Scripts/`（活跃游戏代码）与 `Assets/Script/`（休眠的 JRPG / 侦探框架，122 个文件，经 GUID 检索确认未被任何场景 / 预制体引用）。
- 无 asmdef（全部编译进 Assembly-CSharp）、无测试文件、根目录无 .gitignore。
- UI 视觉主题：维多利亚英伦侦探风（菜单 / HUD / 房间条目已套用 Detective 主题资产）。
- 音频全部由 `AudioManager` 在运行时波形合成，仓库内**无音频文件**。

---

## 1. 项目概述 | Overview

| 项 | 说明 |
|---|---|
| 类型 | HD2D（3D 场景 + 2D 像素精灵角色）双人在线合作解谜，毕业设计 |
| 视觉主题 | 英伦侦探（Victorian detective）：羊皮纸 / 蜡封 / SpecialElite 字体 UI；关卡为"时空试炼之殿" |
| 核心机制 | 压力板 + 滑动门交替通行（脚本就绪、场景待布置）、双人分立出口、ComputeShader 迷雾氛围、游戏内聊天、（传送门系统存在但已禁用） |
| 联机 | 1 Host + 1 Client，Netcode 直连；云端仅做账号与房间"介绍所" |
| 音频 | 运行时波形合成（SFX + BGM），无外部音频资产 |
| 目标平台 | Windows Standalone (PC) |

---

## 2. 技术栈 | Tech Stack

| 项 | 值 |
|---|---|
| 引擎 | Unity 2022.3.62f3c1（Tuanjie 团结引擎） |
| 渲染管线 | URP 14.0.12（`Assets/Settings/` 三档质量资产 + Renderer；Volume Profile：`SampleSceneProfile` 与 `PostProcess/SGS_VolumeProfile`） |
| 网络 | com.unity.netcode.gameobjects 1.12.2 + UnityTransport（UDP 7777） |
| 输入 | `activeInputHandler: 2`（Input Manager 与 Input System **双开**；活跃游戏代码用 legacy `Input.*`，休眠 JRPG 框架用 InputSystem 1.19.0） |
| UI | uGUI + TextMeshPro 3.0.7 |
| 其他包 | addressables 1.29.0（已安装、未见使用）、timeline 1.7.7、visualscripting 1.9.4、test-framework 1.1.33（无测试文件） |
| 包管理注意 | `manifest.json` 含两个**项目外**本地路径依赖：`file:../.codely-cli/...`、`file:../.codely.packages/...`（cn.tuanjie.codely.bridge、cn.tuanjie.ai.generators）——换机 / 移动项目会解析失败 |
| 编译 | 无任何 .asmdef → 全部进 `Assembly-CSharp`（Editor 脚本进 `Assembly-CSharp-Editor`）。**新增类名需全项目唯一** |

---

## 3. 顶层目录地图 | Root Layout

| 路径 | 说明 |
|---|---|
| `Assets/` | 见 §4 |
| `Packages/manifest.json` | 包依赖（注意项目外 file: 路径） |
| `ProjectSettings/` | 含 `EditorBuildSettings.asset`（3 个场景入构建） |
| `server/` | **Node.js 后端**：Express + bcryptjs + jsonwebtoken + SQLite（`db.js`、`routes/`、`middleware/`、`data/`），自带 `.gitignore` 与 `.env.example`；`README.md` 为阿里云部署指南 |
| `Docs/Prompts/` | 提示词文档 |
| `玩法建议.md` | 2026-08-24 的玩法改进清单（部分引用的脚本已被删除，阅读需过滤） |
| `玩家用例图 / 游戏实体图 / 游戏E-R图.drawio` | 设计图 |
| `CODELY.md` | Codely CLI 项目记忆（部分 TODO 已过时） |
| `Backups/` `.history/` `mem-log/` `screenshots/` `Logs/` | 本地产物，勿提交 |
| 根目录 `.gitignore` | **不存在**（只有 `.codelyignore`） |

---

## 4. Assets 目录地图 | Assets Map

| 目录 | 说明 |
|---|---|
| `Scripts/` | **活跃游戏代码**（34 个 .cs，见 §9） |
| `Script/` | ⚠️ **休眠 JRPG / 侦探框架**（单数，122 个 .cs，见 §10），已确认未被场景 / 预制体引用 |
| `Scenes/` | SampleScene / GameScene / Level1 / Level2 / HD_2D_Day（见 §5） |
| `Prefabs/` | `Player 1.prefab`、`player.prefab`（两个玩家网络预制体，均已注册于 `DefaultNetworkPrefabs.asset`）、`RoomItemPrefab.prefab`（房间列表条目） |
| `Prefab/` | ⚠️ 单数目录：`场景物品/`、`地图/`（中文子目录）+ 八方旅人环境 prefab |
| `Materials/` | URP Lit 材质（ChamberFloor / ChamberWall / DoorMat / PlateInactive / CubeMat / ExitZone / Laser* / MirrorMat / PlayerBlue 等） |
| `Material/` | ⚠️ 单数目录：精灵阴影材质 |
| `素材/` | 角色精灵帧（`down/ idle/ left/ right/ up/`）+ **`八方旅人资源/`**（Octopath 风格环境 / 特效 prefab 库：建筑、围栏、瀑布、烟雾、远山等） |
| `Art/` | `UI/` 侦探主题切图（Detective_MenuBG / Panel / Button / PaperStrip / WaxSeal）；`Fonts/`（SpecialElite SDF、NotoSerifSC SDF） |
| `Game Data/` | JRPG 框架的 ScriptableObject 数据（`Battle/`、`character/`）——服务休眠框架 |
| `PostProcess/` | `SGS_VolumeProfile.asset` + `SGS_LUT.png` |
| `迷雾/` | 迷雾系统脚本（FogSimulation / FogObstacle，中文目录名） |
| `Editor/` | 3 个编辑器工具（见 §12） |
| `Settings/` | URP 资产（三档质量 + Renderer + SampleSceneProfile） |
| `Shaders/` | `Pixelation.shader`（Hidden/Pixelation，当前未生效，见 §15） |
| `Animator/Field/` | `FieldBase.controller`（参数 `facing` int / `isMoving` bool；PlayerMove 用 `SetInteger("facing")`：0=idle,1=down,2=up,3=left,4=right） |
| `Terrains/` + 根下 `New Terrain*.asset` | 地形数据 |
| `Codely/`、`TJGenerators/` | Codely CLI 桥接资产、AI 生成历史产物（DetectiveThemeTool 引用其中的生成图） |
| `Audio/`、`Textures/` | 空（音频全为程序化生成） |
| `TextMesh Pro/`、`TutorialInfo/` | 默认资源 |

> ⚠️ **易混目录三对**：`Scripts` vs `Script`、`Materials` vs `Material`、`Prefabs` vs `Prefab`；另有中文目录名（`素材`、`迷雾`、`Prefab/场景物品`、`Prefab/地图`），路径操作时注意编码；prefab 文件名含空格（`Player 1.prefab`）。

---

## 5. 场景与构建 | Scenes & Build

| 场景 | Build | 内容 |
|---|---|---|
| `SampleScene.unity` | Index 0（入口） | 纯菜单：MainMenuCanvas（LoginPanel + RoomPanel）、GameHUDCanvas、AudioManager、NetWorkManager（NGO + UnityTransport）、EventSystem、Global Volume、相机 / 灯光 |
| `GameScene.unity` | Index 1 | "时空试炼之殿" NPC 房：NPC / NPC2 / NPC3、DialogueCanvas（DialoguePanel 等）、ChatNetwork、房间外壳（Floor / Ceiling / Wall）、相机 / 灯光 / EventSystem |
| `Level1.unity` | Index 2 | "第一关：协作之门"：LevelManager、LevelCanvas（StatusText / CompletePanel / CompleteText）、Exit_P1 / Exit_P2、fog（FogSimulation）、ChatNetwork、Plane / Cube 组成的关卡几何。**当前未放置压力板 / 门** |
| `Level2.unity` | **未入构建** | 第二关雏形：复用 LevelManager / LevelCanvas / LockedDoor（1 个）；含数百个编号 `Text (TMP)` 遗留对象待清理 |
| `HD_2D_Day.unity` | **未入构建** | 八方旅人资源美术演示场景 |

**场景加载模型（重要）**：
- 全部通过 `NetworkManager.SceneManager.LoadScene(name, Single)` 由**服务器发起**；客户端跟随。
- **玩家 NetworkObject 跨场景保留**（不随场景卸载销毁，OnNetworkSpawn 只跑一次）→ 每关必须由服务器显式传送出生点：`LevelManager.OnNetworkSpawn` 或 `GameManager.OnSceneLoadComplete` 调 `PlayerMove.ServerTeleport()`。
- `LoadingScreen`（DontDestroyOnLoad）显示全屏覆盖层，进度条为**化妆动画**（Netcode 不暴露加载进度），完成时 `Complete()` 收起。

---

## 6. 完整游戏流程 | Game Flow

1. **启动**（SampleScene）：`UIManager` 检查 `ApiClient.IsLoggedIn`（PlayerVars 中的 JWT）→ 已登录直接进大厅，否则登录 / 注册面板。
2. **大厅 RoomPanel**：`GET /api/rooms` 列出房间；**建房**（自动探测本机 IP → `POST /api/rooms` → `GameManager.StartHost()`，绑 `0.0.0.0:7777`）；**加房**（`POST /api/rooms/:id/join` 取 host_ip → `StartClient`）；或**手输 IP 直连**（跳过 API，`CurrentRoomId=-1`）。
3. 客户端连接 15 秒超时 → 自动 Shutdown 回大厅；断线也回大厅（`ExitRoom`）。
4. Host 侧自动加载 GameScene；`OnSceneLoadComplete` 中（本机 clientId 匹配）收 LoadingScreen、激活 HUD、发送聊天系统消息、服务器把两名玩家传送回殿内出生点。
5. **GameScene**：仅 P1（Host）可走近 NPC 按 **E** 对话（7 段台词）→ 任务选择（任务 2 弹"第二关开发中" Toast 拦截）→ P2 弹确认面板（**30 秒超时**自动取消）→ 确认后服务器 `GameManager.LoadLevel("Level1")`。
6. **Level1**：HUD 显示关卡名 / 目标与 P1/P2 出口状态；两名玩家分别进入 Exit_P1 / Exit_P2 → `LevelManager` 显示完成面板 → 5 秒倒计时（Toast）→ 自动返回 GameScene。
7. **全程可用**：**T** 开聊天（Enter 发送 / Esc 取消）、**Y** 置顶聊天历史、**ESC** 暂停菜单（继续 / 设置(分辨率·全屏·音量) / 退出房间）。退出房间 = `RoomApi.LeaveRoom` + Netcode Shutdown + 回大厅 + MenuBGM。

---

## 7. 网络架构约定 | Networking Conventions（核心）

> **服务器权威模型**：所有游戏状态由服务器写；客户端只发请求。

- **NetworkVariable**：私有字段 `_` 前缀；写权限 `NetworkVariableWritePermission.Server`（或仅在 `IsServer` 分支写）。所有需同步状态的对象必须挂 `NetworkObject`。
- **ServerRpc**：方法名以 `ServerRpc` 结尾；跨端请求用 `[ServerRpc(RequireOwnership = false)]` + `ServerRpcParams.Receive.SenderClientId` 识别真实发送者（勿用 `LocalClientId`）。
- **ClientRpc 定向发送**：`ClientRpcParams.Send.TargetClientIds = new[] { OwnerClientId }`（见 `PlayerMove.ServerTeleport` 模式）。
- **玩家位置同步**（`PlayerMove`）：owner 在 Update 本地移动，**节流 0.1s**（或位移超过阈值）才写 `SyncPos` / 发 `UpdateTransformServerRpc`；非 owner 在 FixedUpdate 用 `SyncPos` 硬覆盖 transform、`SyncFacing` 驱动动画。
  ⚠️ **推论**：任何"本地直接改非 owner 的 transform"都会被下一物理帧拉回。**传送必须走 `ServerTeleport`**（服务器改 SyncPos + 定向 ClientRpc 让 owner 本地生效）。
- **断线对账链**：`OnClientDisconnectCallback` → 客户端自身回大厅；服务器侧 → GameHUD Toast、`LevelManager.OnPlayerDisconnected`（清出口状态）、`NpcDialogue.OnPlayerDisconnected`（取消待确认任务）。
  ⚠️ 玩家对象销毁**不触发 OnTriggerExit** ⇒ 所有基于 Trigger 计数的状态都要在断线回调里对账（压力板计数尚未对账，见 §15）。
- **场景事件**：`OnSceneLoadComplete` 仅处理本机 clientId；`RegisterSceneEvents` 有 `_sceneEventsRegistered` 防重复订阅，`ExitRoom` 退订。
- **动画同步**：`SyncFacing`(int) NetworkVariable，owner 变更时上报（客户端走 `SetFacingServerRpc`）。
- **聊天中继**：`ChatNetwork`（场景内 NetworkBehaviour）`SendChatServerRpc → ReceiveChatClientRpc` 全员广播，携带 senderId 供着色。
- **在线网络对象清单**：Player prefab（自动生成）、LevelManager、LevelExit、PressurePlate、LockedDoor、NpcDialogue、ChatNetwork；休眠的 Puzzle 系列见 §10。

---

## 8. HTTP 后端与 API | Backend

```
Unity 客户端 ──HTTP──→ 云服务器 Node.js API (8.130.65.176:3000) ──SQLite──→ data/
                └────────── Netcode UDP 7777 玩家直连 Host（游戏数据不过服务器）
```

| 端点 | 方法 | 用途 |
|---|---|---|
| `/api/auth/register`、`/api/auth/login` | POST | 注册 / 登录，返回 JWT |
| `/api/rooms` | GET / POST | 房间列表 / 创建房间（body 携带 host 自动探测 IP） |
| `/api/rooms/:id/join`、`/api/rooms/:id/leave` | POST | 加入（返回 host_ip）/ 离开 |
| `/api/rooms/:id` | DELETE | 关闭房间 |
| `/api/health` | GET | 健康检查 |
| TCP **3001** | — | Echo 测试服务（客户端 `EchoTester` 排查连通性用） |

- 客户端基类 `ApiClient`：静态类，UnityWebRequest + 协程回调（`StaticCoroutine`）；`ServerUrl = "http://8.130.65.176:3000"`；JWT 存 PlayerPrefs（key `jwt_token`）；**响应 401 → 清认证 → 自动回登录界面**。
- 服务器 `server/`：bcryptjs **同步**哈希（`hashSync` / `compareSync`）；注册重名返回 **409**；`.env` 需 `JWT_SECRET`（≥32 字符，否则拒绝启动）。
- 房间机制只做"介绍所"：拿到 host_ip 后即 Netcode 直连。

---

## 9. 活跃脚本清单 | Active Scripts（`Assets/Scripts/`，34 个）

**根目录**

| 脚本 | 职责 |
|---|---|
| `GameManager.cs` | Netcode Host/Client 连接、当前房间 ID、15s 连接超时、断线回大厅、Netcode 场景加载调度、`ExitRoom`（退 API 房 + Shutdown + 回大厅 + MenuBGM）；DontDestroyOnLoad 单例 |
| `PlayerMove.cs` | NetworkBehaviour：CharacterController 移动 / 重力 / 朝向动画 / 脚步声 / 相机跟随；`PlayerNumber`、P2 外观运行时替换；同步节流；`ServerTeleport`；聊天开启时抑制输入 |
| `UIManager.cs` | LoginPanel ⇄ RoomPanel 切换、自动登录、菜单淡出；DontDestroyOnLoad 单例 |

**Gameplay/（关卡玩法）**

| 脚本 | 职责 |
|---|---|
| `LevelManager.cs` | NetworkBehaviour 单例；关卡名 / 目标常量（第一关：协作之门）；NetworkVariable 追踪 P1/P2 出口状态；完成面板 + 5s 倒计时回 NPC 殿；断线清出口状态 |
| `LevelExit.cs` | 服务器 Trigger 检测**指定玩家**进入 / 离开对应出口，回调 LevelManager |
| `PressurePlate.cs` | 服务器 Trigger 计数玩家踩踏，NetworkVariable 同步按压态（颜色反馈）；LockedDoor 轮询其 `IsPressed` |
| `LockedDoor.cs` | 服务器每帧轮询关联 plates，NetworkVariable 同步开合，垂直滑动 + 关闭 Collider |
| `NpcDialogue.cs` | P1 专属 E 键对话 → 任务选择 → P2 确认（30s 超时）；NetworkVariable 状态机（-1 未选 / 0 无 / 1 待确认 / 2 确认 / 3 拒绝）；断线取消待确认 |

**Network/（HTTP 客户端 + 聊天中继）**：`ApiClient`（HTTP 基类 / JWT / 401 处理）、`AuthApi`、`RoomApi`、`Models.cs`（`NetworkModels` JsonUtility DTO）、`ChatNetwork`（聊天中继）、`EchoTester`（TCP 3001 连通性测试 UI）。

**UI/**：`LoginPanel`（登录 / 注册 + 本机 IP 显示）、`RoomPanel`（房间列表 / 建房 / 加入 / 直连 / 登出；本机 IP 三重探测：UDP 8.8.8.8 法 → 网卡枚举跳过虚拟网卡 → DNS）、`GameHUD`（谜题名 / 提示 / 玩家标签 / Toast / 完成覆盖层 / **ESC 暂停菜单** / 设置面板(分辨率·全屏·三音量) / FPS 计数；静态 `IsPaused`）、`ChatSystem`（LOL 式聊天：T 开 / Enter 发 / Esc 取消 / Y 置顶历史；8 条 120 字上限 10s 淡出；静态 `IsChatOpen` 供输入抑制）、`LoadingScreen`（场景切换覆盖层，进度为化妆动画）。

**Audio/**：`AudioManager.cs` — DontDestroyOnLoad 单例；运行时波形合成全部 SFX/BGM；调用统一走 `AudioManager.Instance.PlaySfx(name)` / `PlaySfxAt(pos, name, pitchVar)` / `PlayBgm(name)`。已知名称：BGM `MenuBGM`(12s C 大调) / `ChamberBGM`(16s A 小调)；SFX 如 `Connect` / `UIClick` / `Footstep` / `PuzzleComplete`。**Awake 中同步 `GenerateAllClips()`（启动一次性开销）**。

**迷雾/**：`FogSimulation.cs` — ComputeShader 模拟 2D 雾密度场（风 / 玩家推挤 / 漩涡 / `FogObstacle` 障碍半径 / 再生），RenderTexture 双缓冲；每个客户端只模拟包裹自己玩家的雾实例（否则自禁用 + 隐藏 MeshRenderer）。`FogObstacle.cs` — 仅暴露障碍半径。

**HD2D/**：`PixelationFeature.cs` — URP ScriptableRendererFeature + RTHandle/Blitter 写法（**当前未挂载到任何 Renderer，且配套 shader 未修**，见 §15）。

**Portal/（禁用）**：`Portal` / `PortalGun` / `PortalableObject` / `PortalSurface` — 完整的本地传送门实现，**未联网**（传送只改本地 transform，会被 `PlayerMove` 同步拉回），已从玩家预制体禁用。保留待联网化。

**Puzzle/（遗留，未被场景使用）**：`PuzzleManager` / `PuzzleDoor` / `PickupCube`（服务器权威，RPC 用 SenderClientId，位置经 NetworkTransform 复制）/ `ExitZone` / `LaserEmitter` / `LaserReceiver` / `LaserRedirector` — 带网络同步雏形的旧解谜组件，是"第二关素材"（`玩法建议.md` §3）。

---

## 10. 休眠代码与遗留 | Dormant & Legacy

- **`Assets/Script/`（单数，122 个 .cs）**：从参考项目（歧路旅人 JRPG）复制并扩展的框架——
  `MFramework/`（EventBus、Singleton）、`Battle/`（回合制战斗：Command 模式 + 状态机 + Encounter/Skill SO）、`Dialogue/`（SO 对话服务）、`Investigation/`（侦探调查：证物 / 矛盾 / 出示证据 + DemoBootstrap）、`Puzzle/`（SO 线索谜题）、`Game/`（角色 / 队队 / 背包 / 路径行动(打听·招募·商店·偷窃) / 场景切换 / InputSystemController）、`Player/`（PlayerController + PlayerInteractor）、`Network/`（GameNetworkManager + LocalStateSync 存根）、`UI/`（装备 / 背包 / 商店 / 偷窃等 RpgUI 面板）。
  **已验证（GUID 检索 BattleController / InvestigationDemoBootstrap / PlayerController）：未被任何 .unity 场景或 .prefab 引用。** 它们仍会编译进 Assembly-CSharp——新增同名类会冲突；除非用户明确要求，**不要主动接线、重构或删除**。
- `Assets/Scripts/Portal/`：见 §9（禁用中）。
- `Assets/Scripts/Puzzle/`：见 §9（未使用素材）。
- `Assets/Game Data/`：休眠框架的 SO 数据。
- 过时引用示例：`PlayerMove.cs` 中注释提到 `LevelSpawnManager`（该类已不存在，出生点传送由 `LevelManager` / `GameManager` 承担）。

---

## 11. 关键资产 | Key Assets

- **玩家预制体**：`Prefabs/Player 1.prefab`（P1）与 `Prefabs/player.prefab`，均已注册于 `Assets/DefaultNetworkPrefabs.asset`；P2 外观在运行时由 `PlayerMove.ApplyPlayerAppearance` 换 AnimatorController + 精灵。
- **RoomItemPrefab**：大厅房间条目（RoomNameText / HostNameText / PlayerCountText + Join 按钮）。
- **动画**：`Animator/Field/FieldBase.controller`（参数 `facing` int、`isMoving` bool；精灵帧在 `素材/down|idle|left|right|up/`）。
- **侦探主题**：`Art/UI/Detective_*.png`（切图）+ `Art/Fonts/SpecialElite SDF`、`NotoSerifSC SDF`（另 `Assets/Codely/Fonts/NotoSansSC-Regular SDF`）。
- **后处理**：`PostProcess/SGS_VolumeProfile.asset` + `SGS_LUT.png`；`Settings/SampleSceneProfile.asset`。
- **材质**：`Materials/`（URP Lit 系列）；`Material/`（单数，精灵阴影）。
- **地形**：`Terrains/` 与根下 `New Terrain*.asset`。

---

## 12. 编辑器工具 | Editor Tools（`Assets/Editor/`，菜单 `Tools/毕设/`）

| 文件 | 用途 |
|---|---|
| `DetectiveThemeTool.cs`（1222 行） | `Tools/毕设/英伦侦探UI/一键应用`：把生成图切成精灵（Assets/Art/UI）、创建 TMP 字体资产、整体重刷 MainMenuCanvas / GameHUDCanvas / RoomItemPrefab 的侦探主题（含配色常量） |
| `SceneCleanupTool.cs` | 场景清理（曾用于把 SampleScene 清成纯菜单场景） |
| `SetupMenuUI.cs` | 菜单 UI 搭建工具 |

---

## 13. 开发约定 | Conventions

- **命名**：脚本类 PascalCase 且与文件名一致；NetworkVariable 私有字段 `_` 前缀（`_isPressed`）；ServerRpc 方法以 `ServerRpc` 结尾；prefab 文件名保留混合大小写与空格。
- **单例**：`Instance` 静态属性模式；DontDestroyOnLoad 集合 = GameManager、AudioManager、UIManager、GameHUD、ChatSystem、LoadingScreen（+ NetWorkManager、MainMenuCanvas、GameHUDCanvas 场景对象）。单例在 `OnDestroy` 清理 `Instance`（LevelManager 有示范）。
- **网络**：见 §7（服务器权威；`OnNetworkDespawn` 中退订 OnValueChanged——新代码必须遵守）。
- **音频**：一律 `AudioManager.Instance.PlaySfx(name)` / `PlaySfxAt(pos, name)` / `PlayBgm(name)` 字符串名称调用。
- **UI 文案**：面向玩家的字符串为中文（代码内硬编码或 \u 转义混存）；变量与注释英文为主。
- **输入**：活跃游戏用 legacy Input Manager（`Input.GetAxis` / `KeyCode`）；聊天 / 暂停打开时其他输入必须被抑制（查 `ChatSystem.IsChatOpen` / `GameHUD.IsPaused`）。
- **场景对象**：需要网络同步的场景物体 = NetworkObject + NetworkBehaviour（+ 必要 Collider isTrigger）；服务器才做 Trigger 检测。

---

## 14. Tuanjie 2022.3 API 差异 | API Pitfalls

> 该引擎 / 版本的已知差异，写代码前先对照：

- `Rigidbody.velocity`（**不是** `linearVelocity`）
- `NetworkConfig` 无 `NetworkTickRate` 字段
- `DepthOfField` 无 `focusRange` 字段
- `UniversalRenderPipelineAsset` 无 `msaaCount` 字段
- URP 14 Blitter 用 `DrawProcedural`（不绑顶点缓冲）——全屏 Pass 的 shader 需用 `GetFullScreenTriangleVertexPosition(input.vertexID)` + 采样 `_BlitTexture`（`Pixelation.shader` 正因未改而失效）

---

## 15. 已知问题与 TODO | Known Issues（截至 2026-09-11 核验）

**渲染**
1. **HD2D 像素化完全失效**：① `PixelationFeature` 未挂载到任何 URP Renderer（全部 .asset 中无其实例）；② `Pixelation.shader` 仍是旧式顶点（`POSITION` 输入、无 vertexID / `GetFullScreenTriangle`，与 Blitter 不匹配）；③ 片元采样 `_MainTex` 而 Blitter 绑定的是 `_BlitTexture`（TexelSize 恒 0 → 除零）；④ `Shader.Find("Hidden/Pixelation")` 在构建中可能被剔除（应改 `[SerializeField] Shader` 引用或加入 Always Included Shaders）。

**玩法**
2. **Level1 与 NPC 台词不符**：台词描述"踩压力板开门交替通行"，但 Level1 场景未布置任何 PressurePlate / LockedDoor（脚本已就绪；Level2 WIP 已放了 1 个 LockedDoor）。
3. **Level2**：未加入 Build Settings；含数百个编号 `Text (TMP)` 遗留对象需清理。
4. **传送门系统**：脚本完整但未联网、玩家预制体上已禁用（本地传送会被 PlayerMove 同步拉回）。

**联机**
5. **PressurePlate 踩踏计数器**：玩家对象销毁（断线）不触发 OnTriggerExit → `_playersOnPlate` 可能卡死导致门常开；断线对账目前只覆盖 LevelManager 出口状态与 NpcDialogue，**未清压力板**。
6. 房间 / 连接成功时 `RoomPanel.TransitionToGame` 与 `GameManager.OnClientConnected` 会在本机各播一次 `Connect` 音效（可能双响，小事）。

**安全（server/ 已核验）**
7. HTTP 明文传输 + JWT 明文存 PlayerPrefs；注册重名返回 409"用户名已存在"（账号存在性枚举）；bcryptjs 用 `hashSync` / `compareSync` 同步阻塞事件循环。

**音频**
8. `AudioManager.Awake` 同步 `GenerateAllClips()` 合成全部音频（启动一次性卡顿风险）。

**工程**
9. 根目录缺 `.gitignore`（server/ 内有独立一份）。
10. `manifest.json` 引用项目外 `../.codely-cli`、`../.codely.packages` 路径，换机即解析失败。
11. 无 asmdef、无测试文件、无自定义 Build Script；`Scripts/Script`、`Materials/Material`、`Prefabs/Prefab` 三对目录并存。
12. `PlayerMove` 的 `LevelSpawnManager` 注释过时（类已不存在）。

> **已过时的旧 TODO（勿再当真）**：`PlayerMove` 的 spawn 守卫（`_spawned`）、位置同步节流（0.1s）、`ServerTeleport`、断线回大厅、NpcDialogue/PuzzleDoor/LevelManager 的 OnValueChanged 退订等在 CODELY.md 中列为 TODO 的问题**均已修复**。

---

## 16. 运行、构建与测试 | Running & Build

**编辑器运行**（需 HTTP 服务器可达，登录是必经路径）：
1. 打开 `Assets/Scenes/SampleScene.unity` → Play。
2. 一号位：登录 / 注册 → 建房（自动探测 IP，需放行防火墙 UDP 7777）→ 成为 Host。
3. 二号位：另一编辑器实例或构建产物 → 登录 → 房间列表加入，或手输 Host IP 直连（局域网可用）。
4. GameScene 中 P1 按 E 与 NPC 对话选任务，P2 确认后进 Level1。

**本地后端**：`server/` → `npm install` → 复制 `.env.example` 为 `.env`（`JWT_SECRET` ≥32 字符）→ `npm start`（端口 3000；Unity 侧需把 `ApiClient.ServerUrl` 改为本地地址）。云上现役实例：`http://8.130.65.176:3000`（README.md 含阿里云部署指南）。

**构建**：无自定义 Build Script；Build Settings 仅 SampleScene(0) / GameScene(1) / Level1(2)。命令行：`Unity -batchmode -quit -projectPath . -buildTarget Win64 -logFile build.log`。

**测试**：Test Framework 已装但无测试文件；行为验证靠编辑器双开手测。

---

## 17. 给 AI 的操作守则 | Guidance for AI Agents

1. **改活跃游戏代码 → `Assets/Scripts/`**；`Assets/Script/`（单数）是休眠 JRPG / 侦探框架，除非用户明确要求，勿动、勿接线、勿"顺手清理"。
2. **网络改动保持服务器权威**：客户端只发 `ServerRpc(RequireOwnership=false)` 请求，服务器写 NetworkVariable；传送必须走 `ServerTeleport` 模式（本地改非 owner transform 会被同步拉回）。
3. **新增 NetworkBehaviour**：`OnValueChanged` 订阅放在 `OnNetworkSpawn`、退订放在 `OnNetworkDespawn`；Trigger 计数类状态要考虑玩家对象销毁不触发 `OnTriggerExit`（须断线对账）。
4. **Tuanjie 2022.3 API 差异**见 §14，写代码前先对照。
5. **文档时效**：`CODELY.md` 的 TODO 与 `玩法建议.md` 均部分过时（见 §0 / §15），引用前先以代码核验。
6. 路径操作注意：中文目录名（`素材`、`迷雾`）、含空格的 prefab 文件名（`Player 1.prefab`）、三对易混目录（§4）。
7. 用户偏好：重视音频质量与交互打磨；无正当理由不要拆分子代理、不要大范围重构。
