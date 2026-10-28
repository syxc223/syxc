using System.Collections.Generic;
using Game;
using UnityEngine;

/// <summary>
/// 一场战斗的遭遇配置（遭遇组）：出场敌人列表 + 战斗背景 + 阵型。
/// 创建：Project 窗口右键 → Create → Battle → Encounter。
/// 暗雷区域 EncounterZone 持有多个 EncounterSO，按权重随机抽一场（教程 092）。
/// </summary>
[CreateAssetMenu(menuName = "Battle/Encounter", fileName = "Encounter_New")]
public class EncounterSO : ScriptableObject
{
    [Tooltip("本场战斗的敌人（建议 1~4 只）")]
    public List<EnemyDefinitionSO> Enemies = new();

    [Tooltip("战斗背景图（不填则使用深色底）")]
    public Sprite BattleBackground;

    [Tooltip("敌方阵型（站位布局）")]
    public BattleFormation Formation = BattleFormation.Standard;

    [Header("奖励")]
    [Tooltip("击败本场敌人额外获得的金币（敌人 ExpReward 仍各自结算）")]
    [Min(0)] public int BonusMoney = 0;
}
