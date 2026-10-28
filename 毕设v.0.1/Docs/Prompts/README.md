# HD2D 双人合作解密游戏 — AI 开发提示词套件

本套件为你的毕设「HD2D 双人合作解密游戏」提供一整套**可直接复制粘贴**的开发提示词，覆盖代码、后端、美术、音频、关卡与测试全流程。

## 技术栈速览

| 维度 | 选型 |
|---|---|
| 引擎 | Unity 2022.3.62f3c1（团结引擎 Tuanjie）+ URP 14 |
| 联机 | Netcode for GameObjects 1.12.2 + Unity Relay + Unity Transport |
| 数据库 | MySQL + ASP.NET Core (C#) 后端 API |
| 代码 AI | **Codely**（团结引擎 AI 插件，已接入） |
| 美术 AI | **Stable Diffusion**（角色序列帧 / 场景 tile / 道具） |
| 音频 AI | **Suno**（BGM）；音效 SFX 用免版权库 |

## 文件清单与执行顺序

| # | 文件 | 目标工具 | 用途 | 建议时机 |
|---|---|---|---|---|
| — | `README.md` | — | 本索引 | 先读 |
| 00 | `00-总纲与架构.md` | Codely | 一次性喂入项目上下文 + 架构总览 | **第 1 步，必做** |
| 01 | `01-后端与数据库.md` | Codely | ASP.NET Core + MySQL 后端、Unity HTTP 客户端 | 第 2 步 |
| 02 | `02-联机架构.md` | Codely | Netcode + Relay 联机、玩家/谜题同步 | 第 3 步 |
| 03 | `03-谜题玩法.md` | Codely | 谜题联机化 + 双人协作玩法 | 第 4 步 |
| 04 | `04-角色与美术.md` | Stable Diffusion | 角色序列帧、场景 tile、道具、特效 | 可并行，随时 |
| 05 | `05-音频与BGM.md` | Suno | BGM 生成 + SFX 检索方案 | 可并行，随时 |
| 06 | `06-关卡与地图.md` | Codely | 关卡设计 + Tilemap 搭建 | 第 5 步（依赖 04 产出） |
| 07 | `07-UI与打磨.md` | Codely | UI/UX + 后处理光影打磨 | 第 6 步 |
| 08 | `08-测试与验收.md` | Codely | 联机测试清单 + 打包发布 | 最后 |

## 每个提示词块的结构

所有文件内的提示词块统一采用四段式，方便你定位与复制：

```
【目标工具】Codely / Stable Diffusion / Suno
【使用时机】什么时候贴这一段
【提示词正文】>>> 直接复制下面整段 <<<
【预期产出】AI 应返回什么、如何验收
```

## 通用使用规范

1. **给 Codely 的提示词**：贴进 Codely 对话框即可，它已能读取当前工程上下文。每个提示词已注明要修改/新建的具体文件路径，遵循它生成后**先编译验证**（Unity 无报错）再继续下一段。
2. **给 Stable Diffusion 的提示词**：英文提示词已在正文中写死（含负面提示词与参数），直接复制到 SD 的 prompt/negative prompt 框；参数里的 `--` 项（分辨率、采样器、ControlNet）按你本地 SD 版本微调。
3. **给 Suno 的提示词**：Suno 用「风格 + 情绪 + 结构」描述，正文给出可直接粘贴的 `[Style]` 与歌词/纯音乐两种模板。

## 关键架构约定（贯穿所有代码提示词）

- **实时状态走 Netcode**（`NetworkVariable` / `ServerRpc` / `ClientRpc`），**不写数据库**。
- **账号 / 云存档 / 排行榜走 HTTP**：Unity `UnityWebRequest` → ASP.NET Core API → MySQL。
- **团结引擎 2022.3 API 兼容点**（AI 容易写错，已在各提示词中反复强调）：
  - `Rigidbody.velocity`（不是 `linearVelocity`）
  - `NetworkConfig` 无 `NetworkTickRate` 字段
  - `DepthOfField` 无 `focusRange` 字段
  - URP Asset 无 `msaaCount` 字段
