
using Game;

using System.Collections.Generic;
using UnityEngine;
public class ShopAction : ActionBase
{
    [Header("Shop Action")]
    public List<ItemDefinitionSO> itemsBag;

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new PanelRequestEvent(this));
    }

    public bool TryExecuteTransaction(PanelType panelType, ItemDefinitionSO itemDefinition)
{
    var inventory = InventoryManager.Instance;

    if (panelType == PanelType.Buy)
    {
        if (!inventory.TrySpednCurrency(itemDefinition.BuyPrice))
            return false;

        inventory.AddItem(itemDefinition, 1);
        return true;
    }

    if (inventory.GetItemQuantity(itemDefinition) <= 0)
        return false;

    inventory.RemoveItem(itemDefinition, 1);
    inventory.AddCurrency(itemDefinition.SellPrice);
    return true;
}
}