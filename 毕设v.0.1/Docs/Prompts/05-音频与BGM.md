# 05 · 音频与 BGM（Suno + SFX）

> BGM 用 **Suno** 生成；短音效（脚步、机关、按钮）Suno 不适合，用**免版权库**检索 + AI 后期。生成后统一接入现有 `Assets/Scripts/Audio/AudioManager.cs`。

---

## 提示词 5-A — 菜单/主题 BGM

【目标工具】Suno
【使用时机】需要主菜单背景音乐时
【提示词正文】

**Style of Music（风格栏）：**

```
Orchestral fantasy adventure, HD-2D JRPG title theme, warm strings, gentle piano, soft woodwinds, nostalgic, heroic undertone, calm tempo, video game menu music, loop-friendly
```

**Title（标题）：** `Wanderers of the Lost Realm - Menu Theme`

**歌词模式**：选「Instrumental（纯音乐）」，若选歌词则用下面描述：

```
A quiet instrumental overture that opens the door to a world of forgotten ruins and light.
```

【预期产出】一首 1–2 分钟的纯音乐主菜单主题，循环点自然，情绪「温暖 + 冒险感」。循环播放时不突兀即可。

---

## 提示词 5-B — 谜题房氛围 BGM（游戏主循环）

【目标工具】Suno
【使用时机】进入关卡/解谜时
【提示词正文】

**Style of Music：**

```
Atmospheric puzzle room ambient, HD-2D dungeon exploration, subtle strings, soft mallet percussion, ticking clock texture, mysterious but calm, low dynamic, minimalist, loop-friendly, video game puzzle music
```

**歌词模式**：Instrumental

【预期产出】一首低调、循环、不干扰思考的谜题房氛围音乐，带一点「机关/时间」的质感。

---

## 提示词 5-C — 高潮/剧情 BGM（可选）

【目标工具】Suno
【使用时机】最终关卡或剧情演出时
【提示词正文】

**Style of Music：**

```
Epic HD-2D JRPG climax theme, swelling orchestra, powerful brass, driving percussion, emotional strings, rising tension, triumphant resolution, video game boss/finale music
```

**歌词模式**：Instrumental

【预期产出】一首情绪渐强、用于最终关/通关演出的高潮音乐。

---

## 提示词 5-D — 通关/结算 BGM

【目标工具】Suno
【使用时机】通关结算画面
【提示词正文】

**Style of Music：**

```
Warm celebratory JRPG victory theme, gentle triumph, soft bells, light strings, mellow and reflective, short loop, video game clear/results music
```

**歌词模式**：Instrumental

【预期产出】一首温暖、克制、可循环的结算音乐。

---

## 提示词 5-E — SFX 免版权库检索方案

【目标工具】Codely（检索建议）或你自己照做
【使用时机】需要脚步/机关/按钮等短音效时
【提示词正文】

```
Suno 不适合生成短音效（脚步、按钮、机关、传送门），请给我一份「免版权音效库检索清单」，覆盖我这款 HD2D 双人解谜游戏需要的全部音效。要求：

1. 列出推荐音效库：Freesound、ZapSplat、Pixabay Audio、Kenney.nl（免费游戏素材）等，并说明各自版权许可（CC0 / CC-BY / 商用）。
2. 给出每类音效的英文检索关键词：
   - 脚步（木地板/石板）
   - 传送门开启/关闭/穿梭
   - 压力板压下/弹起
   - 机关门打开/关闭
   - 激光通电/命中
   - 拾取方块拿起/放下
   - UI 点击/确认/错误
   - 通关成功提示音
3. 说明后期处理建议：用 Audacity（免费）做降噪、截取、淡入淡出、变调，统一响度（-14 LUFS 左右），导出为 Unity 支持的 wav/ogg。
4. 最后给出这些音效如何接入现有 AudioManager.cs 的建议（用 AudioSource 池、按名字播放）。

请给出完整清单与检索关键词，音效文件我自行下载，接入代码按需再让你写。
```

【预期产出】一份可直接照抄的音效库 + 检索词 + 后期处理清单。

---

## 提示词 5-F — 音频接入 AudioManager（若需代码）

【目标工具】Codely
【使用时机】音效文件导入 Assets/Audio 后
【提示词正文】

```
我已把 BGM 与 SFX 文件放到 Assets/Audio 下。请改造 AudioManager.cs（现有），提供统一音频接口。要求：

1. 播放 BGM：PlayBGM(string clipName)，可循环、可淡入淡出切换（菜单→关卡→结算）。
2. 播放 SFX：PlaySFX(string clipName)，支持多音效叠加（AudioSource 池或 PlayOneShot）。
3. 音量控制：提供 BGM 与 SFX 分开的音量设置接口，供设置界面调用。
4. 用字典/名称索引管理 AudioClip，避免到处硬编码。
5. 兼容点：Unity 2022.3 API，别用 Unity 6 语法。

请给出改造后的 AudioManager.cs 完整代码，并说明在 Inspector 里如何配置 AudioClip 列表。
```

【预期产出】通过名字即可播放 BGM/SFX，支持音量控制与场景切换。
