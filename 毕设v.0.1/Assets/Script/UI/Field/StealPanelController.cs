using System;
using TMPro;
using UnityEngine.UI;

using System.Collections.Generic;
using UnityEngine;
public class StealPanelController : PanelController
{
    [Header("Steal Panel")]
    [SerializeField] private StealItemButton stealItemButtonPrefab;
    [SerializeField] private RectTransform contentRoot;

    [Header("Confirm Popup")]
    [SerializeField] private RectTransform confirmPopup;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private readonly List<StealItemButton> _stealItemButtons = new();
    public override Type PanelActionType => typeof(StealAction);
    private StealAction CurrentStealAction => (StealAction)CurrentAction;
 
    private ItemDefinitionSO _pendingItem;

    public override void SetupPanel(ActionBase actionBase)
    {
        base.SetupPanel(actionBase);

        confirmPopup.gameObject.SetActive(false);

        RefreshItemList();
    }

    private void RefreshItemList()
    {
        ClearItemList();

        foreach (InventoryItem item in CurrentStealAction.itemsToSteal)
        {
            StealItemButton stealItemButton = Instantiate(stealItemButtonPrefab, contentRoot);
            stealItemButton.SetupButton(item, OnpenConfirmPopup);
            _stealItemButtons.Add(stealItemButton);
        }

        if(_stealItemButtons.Count == 0)
        return;

        FirstButton = _stealItemButtons[0].CurrentButton;
        SetDefaultSelection();
    }

    private void OnpenConfirmPopup(ItemDefinitionSO itemDefinition)
    {
        _pendingItem = itemDefinition;
        FirstButton = confirmButton;
        confirmPopup.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);

        popupText.text = $"{itemDefinition.name} 成功率: {itemDefinition.RarityWeight}%";

        SetButtonInteractable(false);

        RebindButtons(confirmButton, OnConfirm);
        RebindButtons(cancelButton, ClosePopup);
        SetDefaultSelection();
    }

    protected override void OnConfirm()
    {
        bool success = CurrentStealAction.TrySteal(_pendingItem);
        popupText.text = success ? "偷窃成功!" : "偷窃失败!";
        cancelButton.gameObject.SetActive(false);
        RebindButtons(confirmButton, ClosePopup);
    }


    private void ClosePopup()
    {
    HidePopup();
    RefreshItemList();
    }

private void HidePopup()
{
    _pendingItem = null;
    confirmPopup.gameObject.SetActive(false);
    SetButtonInteractable(true);
}

    public override bool HandleCancelInput()
    {
        if (confirmPopup.gameObject.activeSelf)
        {
            ClosePopup();
            return true;
        }

        OnCancel();
        return true;
    }

    private void SetButtonInteractable(bool interactor)
{
    foreach (StealItemButton stealItemButton in _stealItemButtons)
    {
        stealItemButton.CurrentButton.interactable = interactor;
    }
}

private void ClearItemList()
    {
        foreach (StealItemButton button in _stealItemButtons)
        
            Destroy(button.gameObject);
        
        _stealItemButtons.Clear();
        FirstButton = null;
    }

}