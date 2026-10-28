using System;
using System.Collections.Generic;

using UnityEngine;
public class InventoryManager : Singleton<InventoryManager>
{
    [Header("Icon Set")]
    public ItemIconSetSO IconSet;

    [Header("Inventory")]
    public List<InventoryItem> CurrentInventory = new ();


    [Header("Currency")]
    public int Currency;
    private int _initialCurrency;

    #region 对外调用接口
    public void AddItem(ItemDefinitionSO itemDefinition, int quantity)
    {
        foreach (var item in CurrentInventory)
        {
            if (item.ItemDefinition != itemDefinition)
                continue;

            item.Quantity += quantity;
            return;
        }

        CurrentInventory.Add(new InventoryItem(itemDefinition, quantity));
    }

    public void RemoveItem(ItemDefinitionSO itemDefinition, int quantity)
{
    for (int i = 0; i < CurrentInventory.Count; i++)
    {
        if (CurrentInventory[i].ItemDefinition != itemDefinition)
            continue;

        CurrentInventory[i].Quantity -= quantity;

        if (CurrentInventory[i].Quantity <= 0)
            CurrentInventory.RemoveAt(i);
        
        return;
    }
}

    public int GetItemQuantity(ItemDefinitionSO itemDefinition)
{
    // 空值校验：如果传入的物品定义为空，直接返回0
    if (itemDefinition == null)
        return 0;

    // 遍历当前背包中的所有物品
    foreach (var item in CurrentInventory)
    {
        // 找到匹配的物品定义，返回该物品的数量
        if (item.ItemDefinition == itemDefinition)
            return item.Quantity;
    }

    // 背包中没有该物品，返回0
    return 0;
}


    public bool TrySpednCurrency(int amount)
{
    if (Currency < amount)
        return false;

    Currency -= amount;
    return true;
}

    internal void AddCurrency(int sellPrice)
    {
        Currency += sellPrice;
    }

    /// <summary>
    /// 汇总共享背包 + 全队已穿戴装备，供主菜单「道具」等界面显示。
    /// </summary>
    public IEnumerable<InventoryItem> GetAllPartyItems()
    {
        var counts = new Dictionary<ItemDefinitionSO, int>();

        foreach (var item in CurrentInventory)
            AddItemCount(counts, item.ItemDefinition, item.Quantity);

        var party = PartyManager.Instance;
        if (party != null)
        {
            foreach (var member in party.PartyMembers)
            {
                foreach (var equipped in member.EquippedItems)
                    AddItemCount(counts, equipped.item, 1);
            }
        }

        foreach (var pair in counts)
            yield return new InventoryItem(pair.Key, pair.Value);
    }

    private static void AddItemCount(Dictionary<ItemDefinitionSO, int> counts, ItemDefinitionSO itemDefinition, int quantity)
    {
        if (itemDefinition == null || quantity <= 0)
            return;

        counts.TryGetValue(itemDefinition, out int existing);
        counts[itemDefinition] = existing + quantity;
    }




    #endregion
}

[System.Serializable]
public class InventoryItem
{
    public ItemDefinitionSO ItemDefinition;
    public int Quantity;

    public InventoryItem(ItemDefinitionSO itemDefinition, int quantity)
    {
        ItemDefinition = itemDefinition;
        Quantity = quantity;
    }

    public bool IsEquipment => ItemDefinition != null && ItemDefinition.itemType == ItemType.Equipment;
    public bool IsConsumable => ItemDefinition != null && ItemDefinition.itemType == ItemType.Consumable;
}