using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopItemButton : ItemButton
{
    [Header("Shop Item Button")]
    [SerializeField] private TMP_Text priceText;

    public void SetupButton(InventoryItem inventoryItem, PanelType panelType, Action<ItemDefinitionSO> onItemClick)
    {
        base.SetupButton(inventoryItem, onItemClick);
        priceText.text = panelType == PanelType.Buy
    ? $"¥{inventoryItem.ItemDefinition.BuyPrice}"
    : $"¥{inventoryItem.ItemDefinition.SellPrice}";


    }
}