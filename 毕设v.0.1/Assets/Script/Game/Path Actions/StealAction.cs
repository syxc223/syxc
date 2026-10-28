using Game;

using System.Collections.Generic;
using UnityEngine;
public class StealAction : ActionBase
{
    [Header("Steal Action")]
    public List<InventoryItem> itemsToSteal;

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new PanelRequestEvent(this));
    }

    public bool TrySteal(ItemDefinitionSO itemDefinitionSO)
   {
    bool success = Random.value <= Mathf.Clamp01(itemDefinitionSO.RarityWeight / 100f);

    if (success)
    {
        InventoryManager.Instance.AddItem(itemDefinitionSO, 1);
        for (int i = 0; i < itemsToSteal.Count; i++)
        {
            if (itemsToSteal[i].ItemDefinition != itemDefinitionSO)
                continue;
            itemsToSteal.RemoveAt(i);
        }
    }

    return success;
    }
}
