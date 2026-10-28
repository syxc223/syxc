using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;

public class EquipmentStatPreviewPanel : MonoBehaviour
{
    [Serializable]
    public struct StatRowBinding
    {
        public StatType statType;
        public EquipmentStatCompareRow row;
    }

    [SerializeField] private StatRowBinding[] statRows;

    public void Refresh(StatBlock currentStats, StatBlock previewStats, bool isInPreviewMode = true)
{
    for (int i = 0; i < statRows.Length; i++)
    {
        var binding = statRows[i];

        int current = ReadStat(currentStats, binding.statType);
        int preview = ReadStat(previewStats, binding.statType);

        binding.row.SetRaw(current, preview, isInPreviewMode);
    }

}
     private int ReadStat(StatBlock statBlock, StatType statType)
{
    switch (statType)
    {
        case StatType.MaxHP: return statBlock.MaxHP;
        case StatType.MaxSP: return statBlock.MaxSP;
        case StatType.PAtk: return statBlock.PAtk;
        case StatType.PDef: return statBlock.PDef;
        case StatType.MAtk: return statBlock.MAtk;
        case StatType.MDef: return statBlock.MDef;
        case StatType.Speed: return statBlock.Speed;
        case StatType.Accuracy: return statBlock.Accuracy;
        case StatType.Evasion: return statBlock.Evasion;
        default: return 0;
    }
}
}