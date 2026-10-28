using System;

using UnityEngine;
/// <summary>
/// 战斗流程的节奏与演出配置（对齐教程 BattleController.Config）。
/// M1 先用默认值；数值都可在 Inspector 调，不必改代码。
/// </summary>
[Serializable]
public class BattleConfig
{
    [Tooltip("进场后到第一次行动的停留时间")]
    public float IntroDuration = 0.8f;

    [Tooltip("每次命中之间的间隔，让玩家看清伤害")]
    public float HitInterval = 0.45f;

    [Tooltip("一个行动者结束到下一个行动者的间隔")]
    public float ActionInterval = 0.35f;

    [Tooltip("逃跑时单位撤回出生点的移动时长（教程 090）")]
    public float EscapeRunDuration = 0.6f;

    [Tooltip("逃跑退场与回场之间的额外停顿（教程 090）")]
    public float EscapeExitDelay = 0.3f;

    [Tooltip("逃跑成功率，1=必定成功")]
    [Range(0f, 1f)] public float EscapeSuccessRate = 1f;
}
