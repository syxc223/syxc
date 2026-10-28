using System.Collections.Generic;
using System.Linq;
using Game;

/// <summary>胜利结算结果（教程 088 结果面板读取这些字段）。</summary>
public class SettlementResult
{
    public int TotalExp;
    public int Money;
    public readonly List<string> LevelUpMessages = new();
    public readonly List<InventoryItem> Drops = new();
}

/// <summary>
/// 独立结算器（教程 087–089）：只在战斗胜利时计算经验/金币/掉落/升级，与战斗流程、UI 解耦。
/// 规则对齐教程 088：存活者平分经验、余数按队伍顺序从前到后各补 1；
/// 阵亡者本场经验为 0，并以 1 血复活（避免卡死在野外）。
/// </summary>
public static class BattleSettlement
{
    public static SettlementResult Settle(
        List<BattleEntity> allies, List<BattleEntity> enemies, EncounterSO encounter)
    {
        var result = new SettlementResult();

        // 1) 敌人经验汇总
        int totalExp = 0;
        foreach (var enemyEntity in enemies)
        {
            if (enemyEntity.Enemy.Definition != null)
                totalExp += enemyEntity.Enemy.Definition.ExpReward;
        }
        result.TotalExp = totalExp;
        result.Money = encounter != null ? encounter.BonusMoney : 0;

        // 2) 存活者平分经验，余数补前
        List<BattleEntity> alive = allies.Where(a => a.IsAlive).ToList();
        if (alive.Count > 0 && totalExp > 0)
        {
            int baseExp = totalExp / alive.Count;
            int remainder = totalExp % alive.Count;

            for (int i = 0; i < alive.Count; i++)
            {
                CharacterRuntimeData member = alive[i].Ally;
                int levelBefore = member.Level;
                int gain = baseExp + (i < remainder ? 1 : 0);

                member.AddExp(gain);

                if (member.Level > levelBefore)
                    result.LevelUpMessages.Add($"{member.DisplayName} 升到了 {member.Level} 级！");
            }
        }

        // 3) 阵亡者不获得经验，复活为 1 血（教程 088）
        foreach (var allyEntity in allies)
        {
            if (!allyEntity.IsAlive)
                allyEntity.Ally.CurrentHP = 1;
        }

        // 4) 掉落：EnemyDefinitionSO 目前没有掉落表字段，M1 留空，M3 扩展掉落表后在此入包。
        return result;
    }
}
