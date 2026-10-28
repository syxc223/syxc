using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 暗雷区域（教程 092）：挂在带 Trigger 碰撞体的区域物体上。
/// 玩家在区域内累计“水平移动距离”，达到随机阈值后由 PlayerEncounterTracker 触发一场随机遭遇。
/// </summary>
public class EncounterZone : MonoBehaviour
{
    [System.Serializable]
    public class WeightedEncounter
    {
        public EncounterSO Encounter;
        [Min(1)] public int Weight = 1;
    }

    [Header("遇敌步距（只计 XZ 水平位移）")]
    [Tooltip("至少走多远才可能遇敌")]
    public float MinEncounterDistance = 8f;
    [Tooltip("走到多远必定进入一次遇敌判定")]
    public float MaxEncounterDistance = 18f;

    [Header("遭遇组（按权重随机）")]
    public List<WeightedEncounter> EncounterTable = new();

    [Tooltip("独立战斗场景引用；留空则使用 BattleService 默认方式（M1 为当前场景叠加）")]
    public AssetReference BattleSceneReference;

    /// <summary>按权重随机抽一场遭遇；表为空返回 null。</summary>
    public EncounterSO GetRandomEncounter()
    {
        if (EncounterTable == null || EncounterTable.Count == 0)
            return null;

        int total = 0;
        foreach (var entry in EncounterTable)
        {
            if (entry != null && entry.Encounter != null)
                total += Mathf.Max(1, entry.Weight);
        }
        if (total <= 0)
            return null;

        int roll = Random.Range(0, total);
        foreach (var entry in EncounterTable)
        {
            if (entry == null || entry.Encounter == null)
                continue;
            roll -= Mathf.Max(1, entry.Weight);
            if (roll < 0)
                return entry.Encounter;
        }
        return EncounterTable[0].Encounter;
    }

    public float GetRandomEncounterDistance()
    {
        return Random.Range(MinEncounterDistance, Mathf.Max(MinEncounterDistance, MaxEncounterDistance));
    }
}
