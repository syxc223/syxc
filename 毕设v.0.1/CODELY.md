# CODELY.md — 毕设v.0.1 项目指南

> HD2D 风格双人合作解谜游戏（毕业设计），基于 Unity 2022.3.62f3c1 (Tuanjie) + URP + Netcode for GameObjects。

---

## 项目概述

| 项目 | 说明 |
|------|------|
| **类型** | HD2D 双人合作解谜游戏（含传送门机制） |
| **引擎** | Unity 2022.3.62f3c1 (Tuanjie / 团结引擎) |
| **渲染管线** | URP (Universal Render Pipeline) 14.0.12 |
| **网络框架** | Netcode for GameObjects 1.12.2 (UnityTransport, 端口 7777) |
| **目标平台** | PC (Windows Standalone) |
| **入口场景** | `Assets/Scenes/SampleScene.unity` (Build Index 0) — **纯菜单场景** |
| **玩法场景** | `Assets/Scenes/GameScene.unity` (Build Index 1) — 连接成功后加载，**玩法内容待补**（见 GameScene 重建清单） |
| **语言** | C# (Assembly-CSharp, 无 asmdef 分包) |

### 核心玩法

1. **双人合作**：一台主机 (Host) + 一台客户端 (Client)，通过 IP 直连。两名玩家需协作完成解谜。
2. **传送门系统**：玩家可用传送门枪 (PortalGun) 在墙壁表面射出蓝/橙两个传送门，物体穿过时保留动量。
3. **解谜机制**：
   - **压力板 (PressurePlate)**：玩家或方块踩上去时激活，控制门的开合。
   - **可拾取方块 (PickupCube)**：按 E 拾取/丢弃，可放置在压力板上。
   - **激光系统 (LaserEmitter/Receiver/Redirector)**：发射器射出激光，经反射镜折射后击中接收器以触发机关。
   - **解谜门 (PuzzleDoor)**：垂直移动门，由压力板或激光接收器控制开合。
   - **出口区域 (ExitZone)**：需两人同时进入才触发谜题完成。
4. **HD2D 视觉**：3D 场景 + 2D 像素风角色精灵 + 像素化后处理 (PixelationFeature)。
5. **程序化音频**：所有 SFX/BGM 均由 `AudioManager` 在运行时通过波形合成生成，无外部音频文件。

---

## 场景结构

> **2026-08-23 调整**：SampleScene 已清理为**纯菜单场景**（原混入的 Map/PuzzleManager/旧 Canvas 已通过 `Tools/毕设/清理 SampleScene` 移除）。所有玩法内容将在 GameScene 中重建，见下文「GameScene 重建清单」。

### SampleScene 层级（纯菜单）

```
SampleScene (Build Index 0)
├── Main Camera              — 50° 俯角
├── Directional Light         — 场景主光源
├── Global Volume             — URP 后处理 Volume
├── NetWorkManager            — NetworkManager + UnityTransport (DontDestroyOnLoad，随 GameScene 复用)
├── EventSystem               — uGUI 事件系统
├── AudioManager              — 程序化音频管理器 (Singleton, DontDestroyOnLoad)
├── MainMenuCanvas            — 主菜单：LoginPanel + RoomPanel (UIManager, DontDestroyOnLoad)
└── GameHUDCanvas             — 游戏 HUD (GameHUD, DontDestroyOnLoad)
    ├── HUDPanel
    ├── PuzzleName (TMP)
    ├── Hint (TMP)
    ├── PlayerLabel (TMP)
    └── CompleteOverlay (inactive)
        └── CompleteText (TMP)
```

### GameScene（Build Index 1，玩法场景待补）

当前只有空房间（Floor/Ceiling/4×Wall/Main Camera/Directional Light/EventSystem）。连接成功后由 `GameManager.LoadGameScene()` 以 Single 模式加载。**玩法内容重建清单见文末。**

---

## 脚本架构

所有脚本位于 `Assets/Scripts/`，无 Assembly Definition 文件，统一编译到 `Assembly-CSharp.dll`。

### 核心系统

| 脚本 | 路径 | 说明 |
|------|------|------|
| `GameManager` | `Scripts/GameManager.cs` | Singleton，管理 Host/Client 连接，端口 7777 |
| `PlayerMove` | `Scripts/PlayerMove.cs` | NetworkBehaviour，CharacterController 移动、重力、动画、脚步声、相机跟随、传送门速度 |
| `UIManager` | `Scripts/UIManager.cs` | 主菜单 / 连接界面，IP 输入、Host/Join 按钮、菜单 BGM |
| `GameHUD` | `Scripts/UI/GameHUD.cs` | 游戏 HUD：谜题名称、提示、音量滑块、完成覆盖层 |
| `AudioManager` | `Scripts/Audio/AudioManager.cs` | Singleton，程序化音频生成、3D 空间音频、SFX 池、BGM 交叉淡入淡出 |

### 传送门系统 (`Scripts/Portal/`)

> ⚠️ **当前已从玩家预制体上禁用**（`Tools/毕设/禁用玩家预制体传送门组件`，2026-08-23）：传送门尚未联网化——PortalGun 创建的传送门是本地普通 GameObject，传送只改本地 transform 且会被 `PlayerMove.SyncTransform` 每物理帧拉回，联机下不可用。脚本全部保留，联网化后用 `Tools/毕设/重新启用玩家预制体传送门组件` 恢复。

| 脚本 | 说明 |
|------|------|
| `Portal` | 传送门触发区，链接伙伴传送门，传送冷却，颜色设置 |
| `PortalGun` | NetworkBehaviour，鼠标左/右键射出蓝/橙传送门，射线检测 PortalSurface，位置网络同步（**待联网化**） |
| `PortalableObject` | 可传送物体组件，处理位置/速度的传送门空间变换（**待联网化**） |
| `PortalSurface` | 标记可放置传送门的表面，法线方向检查 |

### 解谜系统 (`Scripts/Puzzle/`)

| 脚本 | 说明 |
|------|------|
| `PuzzleManager` | NetworkBehaviour Singleton，NetworkVariable 追踪谜题完成状态 |
| `PressurePlate` | NetworkBehaviour，Trigger 检测玩家/方块，NetworkVariable 同步按压状态，联动 PuzzleDoor |
| `PuzzleDoor` | NetworkBehaviour，垂直移动门，NetworkVariable 同步开合状态 |
| `PickupCube` | NetworkBehaviour，按 E 拾取/丢弃；服务器权威，RPC 带 `ServerRpcParams` 识别发送者，位置经 NetworkTransform 复制 |
| `ExitZone` | NetworkBehaviour，需 2 人同时进入触发完成，联动 PuzzleManager |
| `LaserEmitter` | MonoBehaviour，LineRenderer 射线、反射镜折射（最多 10 次反弹）、接收器检测 |
| `LaserReceiver` | NetworkBehaviour，被激光击中时激活联动门，NetworkVariable 同步状态 |
| `LaserRedirector` | MonoBehaviour，反射镜，Vector3.Reflect 计算折射方向 |

### HD2D 后处理 (`Scripts/HD2D/`)

| 脚本 | 说明 |
|------|------|
| `PixelationFeature` | URP ScriptableRendererFeature，像素化后处理 Pass，使用 `Hidden/Pixelation` Shader |

---

## 关键资产

### Prefabs (`Assets/Prefabs/`)

| 文件 | GUID | 说明 |
|------|------|------|
| `Player 1.prefab` | `7eeb68d77a2c3cf48ab49f83bc5de35b` | 网络注册的玩家预制体 (Player 1) |
| `player.prefab` | `c53a6eb47c265f04c8e22e7b5c180763` | 网络注册的玩家预制体 (player) |

> 两个玩家预制体已在 `Assets/DefaultNetworkPrefabs.asset` 中注册。

### URP 配置 (`Assets/Settings/`)

- `URP-Performant.asset` / `URP-Balanced.asset` / `URP-HighFidelity.asset` — 三档渲染质量
- `URP-*-Renderer.asset` — 对应的 Renderer 资产
- `SampleSceneProfile.asset` — 场景 Volume Profile

### 材质 (`Assets/Materials/`)

ChamberFloor, ChamberWall, ChamberAccent, DoorMat, PlateInactive, CubeMat, ExitZone, LaserEmitter, LaserReceiver, MirrorMat, PlayerBlue — 均使用 URP Lit Shader。

### 角色精灵 (`Assets/素材/`)

`down/`, `idle/`, `left/`, `right/`, `up/` — 角色四方向行走 + 待机动画精灵帧。

### 动画 (`Assets/Animator/Field/`)

`FieldBase.controller` — 角色动画控制器，含 `idle-down/left/right/up` 动画。参数：`isMoving` (bool), `moveX` (float), `moveY` (float)。

### Shader (`Assets/Shaders/`)

`Pixelation.shader` — `Hidden/Pixelation`，URP 全屏后处理，按像素块采样降分辨率。

---

## 网络配置

| 配置项 | 值 |
|--------|-----|
| 传输层 | UnityTransport (UDP) |
| 端口 | 7777 |
| Host 绑定 | `0.0.0.0:7777` |
| Client 连接 | `<Host IP>:7777` |
| 网络对象 | Player Prefab (自动生成)、PuzzleManager、PressurePlate、PuzzleDoor、PickupCube、ExitZone、LaserReceiver |
| 同步方式 | NetworkVariable (位置/旋转/状态) + ServerRpc (客户端→服务器请求) |

### 网络同步要点

- `PlayerMove`：服务器持有 `SyncPos`/`SyncRot` NetworkVariable，客户端通过 `UpdateTransformServerRpc` 上报位置。
- `PressurePlate`：仅服务器检测 Trigger，通过 `NetworkVariable<bool> _isPressed` 同步到所有客户端。
- `PuzzleDoor`：`SetOpen()` 仅服务器调用，`NetworkVariable<bool> _isOpen` 同步。
- `PickupCube`（2026-08-23 重做）：服务器权威。RPC 通过 `ServerRpcParams.Receive.SenderClientId` 识别发送者（不再用 `LocalClientId`）；服务器在 Update 中把被持有的方块移向持有者的 `GetPlayerNetworkObject(holderId)` 位置，**位置经 `NetworkTransform` 组件复制**给所有客户端（不再有每帧 MoveCubeServerRpc）。场景中方块物体需挂 `NetworkObject + NetworkTransform + Rigidbody + PickupCube`。
- `PortalGun`：所有传送门位置/旋转/激活状态通过 NetworkVariable 同步（当前在预制体上禁用）。

---

## 构建与运行

### 编辑器运行

1. 打开 `毕设v.0.1.sln` 或通过 Tuanjie Hub 打开项目。
2. 确保活动场景为 `Assets/Scenes/SampleScene.unity`。
3. 按 **Play** 进入运行模式。
4. Host：点击主菜单 "Host" 按钮。
5. Client：在另一实例输入 Host 的 IP 后点击 "Join"。

### CLI / Batchmode 构建

```bash
Unity -batchmode -quit -projectPath . \
       -buildTarget Win64 -logFile build.log
```

> 当前项目无自定义 BuildScript，仅 SampleScene 在 Build Settings 中。

### 测试

- Unity Test Framework 1.1.33 已安装，但当前无测试文件。
- 可通过 `unity_editor` 工具直接控制 Play/Stop 进行手动测试。

---

## 开发约定

### 目录结构

```
Assets/
├── Scripts/          — C# 脚本（按功能子目录组织）
│   ├── Audio/        — 音频系统
│   ├── HD2D/         — HD2D 后处理
│   ├── Portal/       — 传送门系统
│   ├── Puzzle/       — 解谜系统
│   └── UI/           — UI 相关
├── Prefabs/          — 玩家预制体
├── Scenes/           — 场景文件 (.unity)
├── Materials/        — URP 材质
├── Material/         — 精灵阴影材质（注意：单数命名）
├── Shaders/          — 自定义 Shader
├── Settings/         — URP 资产配置
├── Animator/         — 动画控制器和动画片段
├── Textures/         — 纹理（当前为空）
├── Audio/            — 音频文件（当前为空，全部程序化生成）
├── 素材/              — 角色精灵帧素材（中文目录名）
├── TextMesh Pro/     — TMP 资源
└── TutorialInfo/     — Unity 默认教程文件
```

### 命名规范

- **脚本类名**：PascalCase（如 `PlayerMove`、`PuzzleManager`）
- **脚本文件名**：与类名一致
- **Prefab 文件名**：混合大小写（如 `Player 1.prefab`），注意含空格
- **场景**：`SampleScene.unity`（Unity 引擎 `.unity` 扩展名）
- **材质**：PascalCase（如 `ChamberFloor.mat`）
- **精灵目录**：小写英文（`down/`, `idle/`, `left/`, `right/`, `up/`）
- **NetworkVariable**：下划线前缀私有字段（如 `_isPressed`、`_isOpen`）
- **ServerRpc**：方法名以 `ServerRpc` 后缀结尾（如 `UpdateTransformServerRpc`）

### 代码风格

- 使用 `NetworkBehaviour` 而非 `MonoBehaviour` 处理所有需要网络同步的对象。
- 服务器权威模型：所有状态变更由服务器通过 `NetworkVariable` 或 `ServerRpc` 处理。
- Singleton 模式：`GameManager`、`AudioManager`、`PuzzleManager`、`GameHUD` 均使用 `Instance` 单例。
- 音频调用统一通过 `AudioManager.Instance.PlaySfx(name)` / `PlaySfxAt(pos, name)`。
- `AudioManager` 使用 `DontDestroyOnLoad` 持久化。

### URP 注意事项

- 材质统一使用 `Universal Render Pipeline/Lit` Shader。
- 后处理通过 `ScriptableRendererFeature` 实现（见 `PixelationFeature`）。
- Volume Profile 在 `Assets/Settings/SampleSceneProfile.asset`。
- **API 注意** (Unity 2022.3 Tuanjie)：`Rigidbody.velocity`（非 `linearVelocity`），`NetworkConfig` 无 `NetworkTickRate` 字段，`DepthOfField` 无 `focusRange` 字段，`UniversalRenderPipelineAsset` 无 `msaaCount` 字段。

---

## 包依赖列表 (`Packages/manifest.json`)

### 核心 Unity 包

| 包 | 版本 | 用途 |
|----|------|------|
| `com.unity.render-pipelines.universal` | 14.0.12 | URP 渲染管线 |
| `com.unity.netcode.gameobjects` | 1.12.2 | 网络多人框架 |
| `com.unity.textmeshpro` | 3.0.7 | 文本渲染 |
| `com.unity.timeline` | 1.7.7 | 时间线 |
| `com.unity.2d.sprite` | 1.0.0 | 2D 精灵支持 |
| `com.unity.ugui` | 1.0.0 | uGUI |
| `com.unity.test-framework` | 1.1.33 | 测试框架 |
| `com.unity.visualscripting` | 1.9.4 | 可视化脚本 |

### 编辑器集成

| 包 | 版本 | 用途 |
|----|------|------|
| `com.unity.ide.rider` | 3.0.36 | JetBrains Rider 支持 |
| `com.unity.ide.visualstudio` | 2.0.22 | Visual Studio 支持 |
| `com.unity.ide.vscode` | 1.2.5 | VS Code 支持 |
| `com.unity.collab-proxy` | 2.12.4 | Unity Collaborate |
| `cn.tuanjie.codely.bridge` | 1.0.75 | Codely CLI Unity 桥接 |

---

## 版本控制建议

### 应忽略的目录/文件

```
Library/
Temp/
obj/
Build/
Builds/
Logs/
UserSettings/
*.csproj
*.sln
```

### 应纳入版本控制的内容

```
Assets/
ProjectSettings/
Packages/manifest.json
Packages/packages-lock.json
```

> ⚠️ 当前项目**缺少 `.gitignore` 文件**，建议添加标准 Unity `.gitignore`。

---

## GameScene 重建清单（玩法内容待补）

当前 GameScene 是空房间。搭建玩法时需在 GameScene 中放置以下内容（各组件字段与旧 SampleScene 中的实现一致，可参照 `Scripts/Puzzle/`）：

| 物体 | 必需组件 | 要点 |
|------|---------|------|
| `PuzzleManager` | NetworkObject + PuzzleManager | 谜题完成状态中枢 |
| `PressurePlate_1/2` | NetworkObject + Collider(isTrigger) + PressurePlate | `linkedDoors` 指向 PuzzleDoor；玩家踩踏由服务器检测 |
| `PuzzleDoor` | NetworkObject + Collider + PuzzleDoor | 垂直移动门 |
| `PickupCube` | **NetworkObject + NetworkTransform** + Rigidbody + PickupCube | ⚠️ 新增 NetworkTransform（重做后的方块位置复制依赖它） |
| `ExitZone` | NetworkObject + Collider(isTrigger) + ExitZone | 两人同时进入触发完成 |
| 墙面 `PortalSurface` | Collider + PortalSurface | 重新启用传送门时需要（当前传送门已禁用） |

> NetworkManager / AudioManager / MainMenuCanvas / GameHUDCanvas 均 DontDestroyOnLoad，**无需**在 GameScene 中重复放置。

## TODO / 待解决问题

**玩法**
- [ ] **GameScene 玩法内容**：按上方清单重建关卡（当前进入游戏是空房间）。
- [ ] **传送门联网化**（当前已在玩家预制体禁用）：`PortalGun.Awake` 在每台机器的每个玩家实例（含他人副本）上创建本地传送门，传送只改本地 transform，被 `PlayerMove.SyncTransform` 撤销；需改为服务器权威（RequestTeleportServerRpc + 出口速度经网络同步），并修 `Portal.cs` 的 ping-pong 往返传送（OnTriggerStay 0.3s 冷却后被重复触发）与 `Quaternion.LookRotation(hit.normal, Vector3.up)` 在水平表面退化问题。

**渲染**
- [ ] **像素化后处理完全失效（HD2D 核心效果）**：① `PixelationFeature` 未被挂到任何 URP Renderer（当前激活 High Fidelity）；② `Pixelation.shader` 顶点着色器读 `POSITION/TEXCOORD0`，而 URP 14 Blitter 用 `DrawProcedural` 不绑顶点缓冲（应改用 `GetFullScreenTriangleVertexPosition(input.vertexID)`）；③ 片元采样 `_MainTex` 但 Blitter 绑定的是 `_BlitTexture`（`_MainTex_TexelSize` 恒 0 → 除零 NaN）。修法见 `Blitter.cs`（PackageCache core 14.0.12）。
- [ ] `PixelationFeature` 的 `Shader.Find("Hidden/Pixelation")` 在构建中可能返回 null（Hidden shader 被剔除）→ 用 `[SerializeField] Shader` 引用或加入 Always Included Shaders。

**联机稳定性**
- [ ] `PlayerMove.FixedUpdate` 缺 spawn 守卫：spawn 前读 `SyncPos.Value` 抛 `InvalidOperationException`（连接期每物理帧刷异常）；非 owner 每物理帧用 `SyncPos` 硬覆盖 transform 会撤销本地传送。
- [ ] `PlayerMove.UpdateTransform` 每帧写 NetworkVariable + 每帧 `UpdateTransformServerRpc`（~60 RPC/s/玩家）→ 需节流/阈值。
- [ ] 6 个 NetworkBehaviour（PuzzleManager/PressurePlate/PuzzleDoor/PickupCube/ExitZone/LaserReceiver）的 `OnValueChanged` 未在 `OnNetworkDespawn` 退订（PickupCube 已修）→ 重连后回调叠加。
- [ ] `PressurePlate`/`ExitZone` 占用计数器在物体销毁/玩家掉线时不会触发 `OnTriggerExit` → 计数器卡死、谜题锁死。需在 OnNetworkDespawn 对账（HashSet<NetworkObjectId>）。
- [ ] `GameManager`：`RegisterSceneEvents` 重复订阅 `OnSceneLoadComplete`；无 `OnClientDisconnectCallback`（主机退出后客户端卡死在游戏场景）；`OnClientConnectedCallback` 未退订且 Host 自己也会收到 Connect SFX。
- [ ] 单例（PuzzleManager/GameHUD 等）`OnDestroy` 不清 `Instance`，场景卸载后静态引用指向已销毁对象。

**解谜系统**
- [ ] `LaserEmitter` 每帧 `FindObjectsByType<LaserReceiver>`（全场景扫描+分配）；且每个发射器每帧重置**所有**接收器的 `_currentlyHit` → 多个发射器互相打架。改为接收器自管 last-hit 帧号。
- [ ] `LaserEmitter.SetActive(false)` 不重置已命中的接收器 → 门保持打开。
- [ ] `LaserEmitter`/`PressurePlate`/`ExitZone`/`LaserReceiver` 激活时 `EnableKeyword("_EMISSION")` 从不 DisableKeyword/复位 → 关闭后仍发光。

**音频与 UI**
- [ ] `AudioManager.Awake` 同步生成 ~130 万采样点音频（启动卡顿）；`CrossfadeBgm` 可重入（两个协程抢同一对 AudioSource）；`Footstep2` 与 `Footstep` 是完全相同的重复 clip。
- [ ] `GameHUD` 音量滑块每次场景加载硬编码覆盖用户设置（应从 AudioManager 读取）；ESC 在完成覆盖层显示时先开暂停菜单而非关闭覆盖层。

**工程**
- [ ] **缺少 `.gitignore`**（项目还不是 git 仓库）：Library/Temp/Logs/UserSettings/screenshots/server/node_modules/server/data/*.csproj/*.sln 均需忽略。
- [ ] `Packages/manifest.json` 引用项目外的 `../.codely-cli` 与 `../.codely.packages` 路径，复制项目到别的机器会解析失败。
- [ ] 无 Assembly Definition（编译到 Assembly-CSharp）；无自定义 Build Script；无测试文件。
- [ ] `Material/`（单数）与 `Materials/`（复数）两个目录并存，建议合并。
- [ ] 服务器侧：JWT 存 PlayerPrefs 明文 + HTTP 明文传输（需 HTTPS 反代 + 部署指南）；登录/注册用同步 bcrypt（阻塞事件循环）；注册接口 409 泄露账号存在性。

---

## Codely Structured Memories

### User
- [2026-08-05 19:59:30] User is building a graduation project (毕设) — an HD2D-style co-op puzzle game in Unity 2022.3.62f3c1 (Tuanjie). Uses URP + Netcode for GameObjects. Prefers no sub-agents. Emphasizes audio quality and interaction polish. Reference project (PlayerController with InputSystemController) is at F:\UnityProgram\歧路旅人.0.2（战斗未完成）\歧路旅人.0.5.

### Feedback
- [2026-10-23 13:26:24] Unity 桥接工具用法（2026-10-23 实测，bridge 1.0.87+）：exec_runtime_script 里顶层 await 会死锁/被拒——多帧验证一律"返回 IEnumerator 协程 + Debug.Log 输出 + 工具 logs 收集"，每次跑前先 unity_editor stop 清掉上一场 Play 残留（否则场景里会出现多个 NetworkManager、端口 7777 被占）。反射调用 ServerRpc 伪造非本机 SenderClientId 会被 NGO 生成代码拦截——要测服务器逻辑就把逻辑抽成普通方法（如 KeyPuzzleManager.HandleDoorPress）直连调用，RPC 只留薄壳。

### Project
- [2026-10-06 15:41:14] 2026-10-06 建成 Level3《双生仓库》协作关卡（设计/接线/流程/验证见 Docs/Level3-双生仓库-设计.md；改动前场景备份在 Backups/Level3_pre_coop.unity）。关键不变量：可搬运箱必须在 Carryable(9) 层、传递窗屏障在 CoopChute(10) 层，且两层碰撞矩阵已忽略——错层会导致传递窗机制失效。Level3 已补 ChatNetwork、LevelManager、CameraBounds、EventSystem（Level1 是同款骨架，可作模板）。
- [2026-10-08 20:32:52] 毕业论文《基于Unity3D的多人协作解密游戏设计与实现》（王轶博，2026-05 提交）位于 F:\Schoolword\论文\论文\论文最新版.docx；用户明确要求**不得改动该 docx**，新增章节以独立文件交付供其自行筛选（已交付 第4章-系统详细设计与实现-草稿.md，正文段落可直接粘贴、【作图说明】区块仅供画图不入文）。
- [2026-10-12 16:37:55] 2026-10-09 交付论文第5章草稿 F:\Schoolword\论文\论文\第5章-系统测试-草稿.md：5.3 功能测试为 7 张表（表5-1～表5-7，72 条用例，含【作图说明】区块不入文）；5.4 非功能测试按要求改为纯段落式结论、无表格无编号用例。注意：论文最新版.docx 第4章实际只有 6 个模块（用户／房间管理／网络／NPC交互与任务选择／谜题判定与关卡流程／UI与聊天），第5章测试用例必须按这 6 个模块对应，而非第4章草稿的 8 模块。另：Assets/Scenes/Level2.unity 仅有 LevelManager+LockedDoor（无 LevelExit／压力板／ChatNetwork），不可通关，第5章测试范围因此限定为主菜单、大厅、Level1、Level3。
- [2026-10-17 14:59:04] 项目文档时效（2026-10-17 核验）：AGENTS.md(2026-09-11)与 CODELY.md 中"玩法场景＝GameScene"已过时——实际玩法场景是 Assets/Scenes/大厅.unity（NPC 殿，Build 1，含 NPC/NPC2/NPC3 三个 NpcDialogue 分别指向 Level1/Level2/Level3）；Build Settings 现为 SampleScene(0)/大厅(1)/Level1(2)/Level2(3)/Level3(4)。另经 vfs_refs 核验：Assets/Scripts 为活跃代码(40 个 .cs)，Assets/Script(单数，122 个)休眠未引用，且 Portal/* 与 Scripts/Puzzle/ 的 PuzzleManager/PuzzleDoor/ExitZone/Laser* 也是 0 场景引用（仅 PickupCube 被 Level3 复用）。
- [2026-10-23 15:27:01] 2026-10-23 完成 Level1《消失的钥匙》协作化（详见 Docs/Level1-协作化-改动与论文影响.md，备份 Backups/Level1_pre_coop.unity）：线索按 _playerOnly 拆半（P1:门前告示[私有关键线索:锁孔空]/桌子/柜子/箱子6,7；P2:壁画[私有关键线索:金漆未干]/地板/箱子8,9,10；NPC与墙上文字皆可）。演示模式（2026-10-23 追加）：金字门槛=_searchesToReveal(场景值 4，0=全部必查)；推门默认单人一次 E 可开，"两人3秒同按"保留为 _requireBothPlayersToPush 开关（默认关）。出口闸门在北半间 z≈14（拱口被"大门"占用，SM_Door01 的 BoxCollider 原本 disabled 是既有 bug 已修）。论文第4/5章需按文档第6节同步。


### Reference
- [2026-08-05 19:59:30] Unity 2022.3 (Tuanjie) API notes: Rigidbody.velocity (not linearVelocity), NetworkConfig has no NetworkTickRate field, DepthOfField has no focusRange field, UniversalRenderPipelineAsset has no msaaCount field.
- [2026-10-08 20:32:56] 论文第4章按 8 个核心功能模块撰写（用户／房间管理／网络／玩家控制／NPC交互与任务选择／机关与道具交互／谜题判定与关卡流程／UI与聊天），与第2章用例一一对应；第5章功能测试必须与第四章模块对应（≥5 个）。论文插图用 draw.io，文件与 docx 同目录（F:\Schoolword\论文\论文\），命名形如 图4-1-用户模块时序图.drawio。
- [2026-10-24 22:34:40] 代码讲解类文档索引（先读再决定是否通读代码）：Docs/代码逻辑讲解.md（按论文 6 模块逐脚本讲解，含设计模式、已知问题、10 分钟讲解顺序）；Docs/代码逻辑讲解-答辩速查.md（模块→文件→方法表、18 条 Q&A、参数表、术语对照）；Docs/联机与数据库-实现说明.md（侧重"如何实现"，联机 8 步链路 + SQLite 9 步落地 + 衔接时序 + 一致性安全表 + 附录作图说明/工程备注，可直接摘入论文第 4 章）。

