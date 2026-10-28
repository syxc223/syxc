using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ItemPanelController : PanelController
{
    [Header("Item Panel")]
    [SerializeField] private ItemButton itemButtonPrefab;
    [SerializeField] private Transform itemButtonParent;

    private readonly List<ItemButton> _itemButtons = new();
    private PanelType _currentPanelType;

    private Action<ItemDefinitionSO> _onItemClick;

   public void SetupPanel(PanelType panelType, ActionBase actionBase = null, Action<ItemDefinitionSO> onItemClick = null)
    {
        if (actionBase != null)
        base.SetupPanel(actionBase);

    _currentPanelType = panelType;
    _onItemClick = onItemClick;

     
     ClearItemListView();
     var inventoryManager = InventoryManager.Instance;
     switch (panelType)
{
    case PanelType.Buy:
         BuildBuyList(inventoryManager);
        break;
    case PanelType.Sell:
        BuildSellList(inventoryManager);
        break;
    case PanelType.Item:
        BuildItemList(inventoryManager);
        break;
   }

      if (_itemButtons.Count == 0)
            return;

        FirstButton = _itemButtons[0].CurrentButton;
        SetDefaultSelection();
}

    private void ClearItemListView()
    {
    // Clear the item list view
    foreach (var itemButton in _itemButtons)
    {
        Destroy(itemButton.gameObject);
    }
    _itemButtons.Clear();
    FirstButton = null;
    }

     private void BuildBuyList(InventoryManager inventory)
    {
        ShopAction shopAction = (ShopAction)CurrentAction;
        foreach(var item in shopAction.itemsBag)
        {
            int owendQuantity = inventory.GetItemQuantity(item);
            AddItemButton(new InventoryItem(item, owendQuantity));
        }
    }

    private void BuildSellList(InventoryManager inventory)
    {
        //已经装备好物品无法被卖出
        foreach(var item in inventory.CurrentInventory)
        {
            AddItemButton(new InventoryItem(item.ItemDefinition, item.Quantity));
        }
    }


    private void BuildItemList(InventoryManager inventory)
    {
        foreach (var item in inventory.GetAllPartyItems())
            AddItemButton(item);
    }

    private void AddItemButton(InventoryItem inventoryItem, bool interactable = true, bool equippedNameFormat = false)
    {
        ItemButton itemButton = Instantiate(itemButtonPrefab, itemButtonParent);

    if (itemButton is ShopItemButton shopItemButton)
        shopItemButton.SetupButton(inventoryItem, _currentPanelType, _onItemClick);
    else
        itemButton.SetupButton(inventoryItem, _onItemClick);

        itemButton.CurrentButton.interactable = interactable;
        _itemButtons.Add(itemButton);
    }

    public void RefreshItemQuantity(ItemDefinitionSO itemDefinition)
    {
        var itemButton = _itemButtons.Find(b => b.CurrentItemDefinition == itemDefinition);
        if (itemButton == null)
            return;

        int newQuantity = InventoryManager.Instance.GetItemQuantity(itemDefinition);
        itemButton.UpdateQuantity(newQuantity);
    }

    internal void RemoveItemButton(ItemDefinitionSO pendingItem)
{
    int targetIndex = _itemButtons.FindIndex(b => b.CurrentItemDefinition == pendingItem);
    if (targetIndex < 0)
        return;

    ItemButton targetButton = _itemButtons[targetIndex];
    _itemButtons.RemoveAt(targetIndex);
    Destroy(targetButton.gameObject);

    //先继续出下一个按钮是什么，再删除当前按钮
    //重置FirstButton
    FirstButton = _itemButtons.Count > 0 ? _itemButtons[0].CurrentButton : null;

    if (_itemButtons.Count > 0)
    {
        int newIndex = Mathf.Min(targetIndex, _itemButtons.Count - 1);
        FirstButton = _itemButtons[newIndex].CurrentButton;
        SetDefaultSelection();
    }
}
}