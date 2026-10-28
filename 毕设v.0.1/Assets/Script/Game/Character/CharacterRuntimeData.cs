using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class CharacterRuntimeData
    {
        public CharacterDefinitionSO Definition;

        public int Level;
        public int CurrentHP;
        public int CurrentSP;

        public int CurrentExp;
        public string DisplayName => Definition != null ? Definition.Name : string.Empty;

        public StatBlock EquipmentStats;

        private bool hasAppliedInitialEquipment;

        [Serializable]
        public struct EquippedItemEntry
        {
            public EquipSlot slot;
            public EquipmentItemSO item;
        }

        public List<EquippedItemEntry> EquippedItems = new();

        public CharacterRuntimeData(CharacterDefinitionSO definition)
        {
            Definition = definition;
            EquipmentStats = StatBlock.Zero;
            Level = definition != null ? definition.BaseLevel : 1;

            var stats = GetBaseStats();
            CurrentHP = stats.MaxHP;
            CurrentSP = stats.MaxSP;

            ApplyInitialEquipment();
        }

        public StatBlock GetBaseStats()
        {
            if (Definition is AllyDefinitionSO allyDef)
                return allyDef.GetStatForLevel(Level);

            return Definition != null ? Definition.BaseStats : StatBlock.Zero;
        }

        public StatBlock GetTotalStats() => GetBaseStats() + EquipmentStats;

        /// <summary>获得经验值，达到条件自动升级（升级回满HP/SP）</summary>
        public void AddExp(int amount)
        {
            if (Definition == null || amount <= 0)
                return;

            CurrentExp += amount;
            while (CurrentExp >= GetExpToNextLevel())
            {
                CurrentExp -= GetExpToNextLevel();
                Level++;
                HealFull();
            }
        }

        /// <summary>升到下一级所需经验：20 × 当前等级²</summary>
        public int GetExpToNextLevel() => 20 * Level * Level;

        public void HealFull()
        {
            StatBlock total = GetTotalStats();
            CurrentHP = total.MaxHP;
            CurrentSP = total.MaxSP;
        }

        public void ModifyHP(int amount)
        {
            CurrentHP += amount;
            CurrentHP = Mathf.Clamp(CurrentHP, 0, GetTotalStats().MaxHP);
        }

        public void ModifySP(int amount)
        {
            CurrentSP += amount;
            CurrentSP = Mathf.Clamp(CurrentSP, 0, GetTotalStats().MaxSP);
        }

        #region 装备系统

        public void ApplyInitialEquipment()
        {
            if (hasAppliedInitialEquipment)
                return;

            if (Definition is not AllyDefinitionSO allyDef)
            {
                hasAppliedInitialEquipment = true;
                return;
            }

            if (allyDef.InitialEquipment == null || allyDef.InitialEquipment.Count == 0)
            {
                hasAppliedInitialEquipment = true;
                return;
            }

            if (InventoryManager.Instance == null)
                return;

            foreach (var entry in allyDef.InitialEquipment)
            {
                if (entry.item == null)
                    continue;

                SetEquippedItem(entry.slot, entry.item);
            }

            hasAppliedInitialEquipment = true;
        }

        public void SetEquippedItem(EquipSlot slot, EquipmentItemSO item)
        {
            int index = EquippedItems.FindIndex(e => e.slot == slot);

            if (index >= 0)
            {
                if (item == null)
                    EquippedItems.RemoveAt(index);
                else
                    EquippedItems[index] = new EquippedItemEntry { slot = slot, item = item };
            }
            else if (item != null)
            {
                EquippedItems.Add(new EquippedItemEntry { slot = slot, item = item });
            }

            RebuildEquipmentStats();
        }

        public EquipmentItemSO GetEquippedItem(EquipSlot slot)
        {
            int index = EquippedItems.FindIndex(e => e.slot == slot);
            return index >= 0 ? EquippedItems[index].item : null;
        }

        public int GetEquippedItemCount(ItemDefinitionSO targetItem)
        {
            if (targetItem == null)
                return 0;

            int count = 0;
            foreach (var entry in EquippedItems)
            {
                if (entry.item == targetItem)
                    count++;
            }

            return count;
        }

        public void RebuildEquipmentStats()
        {
            StatBlock merged = StatBlock.Zero;

            for (int i = EquippedItems.Count - 1; i >= 0; i--)
            {
                EquippedItemEntry entry = EquippedItems[i];
                if (entry.item == null)
                {
                    EquippedItems.RemoveAt(i);
                    continue;
                }

                merged += entry.item.statBonus;
            }

            EquipmentStats = merged;

            StatBlock total = GetTotalStats();
            CurrentHP = Math.Min(CurrentHP, total.MaxHP);
            CurrentSP = Math.Min(CurrentSP, total.MaxSP);
        }

        #endregion
    }
}
