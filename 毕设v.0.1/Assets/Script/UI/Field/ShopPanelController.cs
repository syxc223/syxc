using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Game;

public class ShopPanelController : PanelController
{
    [Header("一级按钮与金额")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text currencyAmountText;

    [Header("二级列表")]
    [SerializeField] private ItemPanelController itemPanel;

    [Header("交互区域")]
    [SerializeField] private CanvasGroup leftPart;
    [SerializeField] private CanvasGroup itemPanelCanvasGroup;

    [Header("Confirm Popup")]
    [SerializeField] private RectTransform confirmPopup;
    [SerializeField] private TMP_Text popupTitle;
    [SerializeField] private TMP_Text popupText;
    [FormerlySerializedAs("confirmButton")]
    [SerializeField] private Button popupConfirmButton;
    [FormerlySerializedAs("cancelButton")]
    [SerializeField] private Button popupCancelButton;

    public override Type PanelActionType => typeof(ShopAction);
    private ItemDefinitionSO _pendingItem;

    private PanelType _currentShopType;

    private ShopAction CurrentShopAction => (ShopAction)CurrentAction;

    private void Awake()
   {
    RebindButtons(buyButton, OpenBuyPanel);
    RebindButtons(sellButton, OpenSellPanel);
    RebindButtons(popupConfirmButton, ExecuteTransaction);
    RebindButtons(popupCancelButton, CloseConfirmPopup);
    confirmPopup.gameObject.SetActive(false);
   }

   public override void SetupPanel(ActionBase actionBase)
   {
    base.SetupPanel(actionBase);
    SetDefaultSelection();
    UpdateCurrencyDisplay();
   }

   private void OpenBuyPanel()
   {
    OpenItemPanel(PanelType.Buy);
   }

   private void OpenSellPanel()
   {
    OpenItemPanel(PanelType.Sell);
   }

   private void OpenItemPanel(PanelType panelType)
{
    _currentShopType = panelType;

    if (itemPanel == null)
        return;

    // Item Panel may start inactive in the scene; activate before spawning rows so Awake runs and the panel is visible.
    itemPanel.gameObject.SetActive(true);
    if (itemPanelCanvasGroup != null)
    {
        itemPanelCanvasGroup.interactable = true;
        itemPanelCanvasGroup.blocksRaycasts = true;
    }

    itemPanel.SetupPanel(panelType, CurrentAction, OpenConfirmPopup);
}

private void OpenConfirmPopup(ItemDefinitionSO itemDefinition)
{
    _pendingItem = itemDefinition;
    confirmPopup.gameObject.SetActive(true);
    itemPanelCanvasGroup.interactable = false;

    //装饰弹出窗口
    if(_currentShopType == PanelType.Buy)
    SetupBuyPopup(itemDefinition);
    else
    SetupSellPopup(itemDefinition);
}
private void SetupBuyPopup(ItemDefinitionSO itemDefinition)
{
    bool canAfford = InventoryManager.Instance.Currency >= itemDefinition.BuyPrice;

    popupTitle.text = "确认购买以下物品吗？：";
    popupText.text = $"{itemDefinition.ItemName}\n价格：¥{itemDefinition.BuyPrice}";

    popupConfirmButton.interactable = canAfford;
    (canAfford ? popupConfirmButton : popupCancelButton).Select();
}

private void SetupSellPopup(ItemDefinitionSO itemDefinition)
{
    popupTitle.text = "确认出售以下物品吗？：";
    popupText.text = $"{itemDefinition.ItemName}\n获得：¥{itemDefinition.SellPrice}";
    popupConfirmButton.interactable = true;
    popupConfirmButton.Select();
}


private void CloseConfirmPopup()
{
    confirmPopup.gameObject.SetActive(false);
    _pendingItem = null;

    if (itemPanelCanvasGroup != null)
        itemPanelCanvasGroup.interactable = true;

    if (itemPanel != null && itemPanel.gameObject.activeInHierarchy)
        itemPanel.SetDefaultSelection();
}

public override bool HandleCancelInput()
{
    if (confirmPopup.gameObject.activeSelf)
    {
        CloseConfirmPopup();
        return true;
    }

    if (!itemPanel.gameObject.activeSelf)
    {
        OnCancel();
        return true;
    }

    itemPanel.gameObject.SetActive(false);
    leftPart.interactable = true;
    _pendingItem = null;
    FirstButton = _currentShopType == PanelType.Buy ? buyButton : sellButton;
    SetDefaultSelection();
    return true;
}

   private void UpdateCurrencyDisplay()
{
    if (currencyAmountText == null)
        return;

    var inventory = InventoryManager.Instance;
    int currentMoney = inventory.Currency;
    currencyAmountText.text = $"持有金额: {currentMoney.ToString()}";
}

    private void ExecuteTransaction()
{
    //Action来触发真实交易
    CurrentShopAction.TryExecuteTransaction(_currentShopType, _pendingItem);
    confirmPopup.gameObject.SetActive(false);
    itemPanelCanvasGroup.interactable = true;

    UpdateCurrencyDisplay();

    //itemPanel刷新持有数
    if (_currentShopType == PanelType.Buy)
    {
        itemPanel.RefreshItemQuantity(_pendingItem);
        itemPanel.SetDefaultSelection();
        return;
    }

    //卖的光了itemPanel里移除按钮
    //Implement with Codex

    //TODO:装备有关不考虑
    var remaining = InventoryManager.Instance.GetItemQuantity(_pendingItem);
    if (remaining > 0)
    {
        itemPanel.RefreshItemQuantity(_pendingItem);
        itemPanel.SetDefaultSelection();
        return;
    }

     itemPanel.RemoveItemButton(_pendingItem);
}
}