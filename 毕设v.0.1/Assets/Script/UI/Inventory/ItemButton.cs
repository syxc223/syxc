using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using UnityEngine;
public class ItemButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemDescription;
    [SerializeField] private TMP_Text itemQuantity;
    [SerializeField] private GameObject itemTips;

    protected Button _button;
    private bool _clickListenerAdded;

    public Button CurrentButton
    {
        get
        {
            EnsureButtonResolved();
            return _button;
        }
    }

    protected void EnsureButtonResolved()
    {
        if (_button != null)
            return;

        _button = GetComponent<Button>();
        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        if (_button != null && !_clickListenerAdded)
        {
            _button.onClick.AddListener(OnClick);
            _clickListenerAdded = true;
        }
    }

    protected InventoryItem _currentItem;
    public ItemDefinitionSO CurrentItemDefinition => _currentItem.ItemDefinition;

    private Action<ItemDefinitionSO> _onItemClick;


    protected virtual void Awake()
    {
        EnsureButtonResolved();
    }

    public virtual void SetupButton(InventoryItem inventoryItem) => SetupButton(inventoryItem, null);
    public virtual void SetupButton(InventoryItem inventoryItem, Action<ItemDefinitionSO> onItemClick)
{
    _currentItem = inventoryItem;
    _onItemClick = onItemClick;

    itemIcon.sprite = InventoryManager.Instance.IconSet.GetIcon(inventoryItem.ItemDefinition.itemIconKey);
    itemName.text = inventoryItem.ItemDefinition.ItemName;
    itemDescription.text = inventoryItem.ItemDefinition.ItemDescription;

    if (itemQuantity != null)
        itemQuantity.text = inventoryItem.Quantity.ToString();
}


    protected virtual void OnClick()
    {
        if (_onItemClick != null)
            _onItemClick.Invoke(CurrentItemDefinition);
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (_currentItem != null)
            _currentItem.Quantity = newQuantity;

        if (itemQuantity != null)
            itemQuantity.text = newQuantity.ToString();
    }
     
    public void OnDeselect(BaseEventData eventData)
    {
        itemTips.SetActive(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        itemTips.SetActive(true);
    }
}