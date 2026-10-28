using System.Collections.Generic;

public static class EquipmentService
{
    public static List<EquipmentItemSO> BuildCandidates(InventoryManager inventory, EquipSlot targetSlot)
    {
        List<EquipmentItemSO> result = new();

        for (int i = 0; i < inventory.CurrentInventory.Count; i++)
        {
            var item = inventory.CurrentInventory[i];
            if (item == null || item.Quantity <= 0)
                continue;

            if (item.ItemDefinition is not EquipmentItemSO equipmentItem)
                continue;

                if(!IsSlotcompatible(equipmentItem, targetSlot))
                    continue;

            result.Add(equipmentItem);
        }
        return result;
    }
    private static bool IsSlotcompatible(EquipmentItemSO item, EquipSlot targetSlot)
{
    if (item == null)
        return false;

        return item.category switch
        {
            EquipmentCategory.Weapon => targetSlot == (EquipSlot)((int)item.weaponType - 1),
            EquipmentCategory.Shield => targetSlot == EquipSlot.Shield,
            EquipmentCategory.Head => targetSlot == EquipSlot.Head,
            EquipmentCategory.Body => targetSlot == EquipSlot.Body,
            EquipmentCategory.Accessory => targetSlot == EquipSlot.Accessory1 || targetSlot == EquipSlot.Accessory2,
            _ => false,
        };
    }

    public static int GetAvailableItemCount(InventoryManager inventory, PartyManager partyManager,ItemDefinitionSO item)
    {
       if (item is null)
        return 0;

    int totalQuantity = inventory.GetItemQuantity(item);
    if (totalQuantity <= 0)
        return 0;

    int equippedCount = 0;
    for (int i = 0; i < partyManager.PartyMembers.Count; i++)
    {
        equippedCount += partyManager.PartyMembers[i].GetEquippedItemCount(item);
    }
    return totalQuantity - equippedCount;
    }
}