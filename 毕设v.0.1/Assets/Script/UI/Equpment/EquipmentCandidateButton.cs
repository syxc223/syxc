using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using Game;
using System;
using UnityEngine;

public class EquipmentCandidateButton : MonoBehaviour,ISelectHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text ownedCountText;

    [Header("Color")]
    [SerializeField] private Color normalTextColor;
    [SerializeField] private Color disabledTextColor;

   
    private Button _button;

    private int _index;

    public Action<int> OnSelect;


    public Button Button => _button;


    private void Awake()
    {
        _button = GetComponent<Button>();
    }


    public void Setup(int index, EquipmentItemSO item, int ownedCount, bool isInteractable, ItemIconSetSO iconSet)
    { 
    _index = index;
    _button.interactable = isInteractable;

// itemNameText.text = item != null ? item.name : "无装备";
        itemNameText.text = item != null ? item.ItemName : "无装备";
        ownedCountText.text = item != null ? ownedCount.ToString() : "-";

    if (item != null)
    {
        itemIcon.sprite = iconSet.GetIcon(item.itemIconKey);
        itemIcon.enabled = true;
    }
    else
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false;
    }

    itemNameText.color = isInteractable ? normalTextColor : disabledTextColor;
    ownedCountText.color = isInteractable ? normalTextColor : disabledTextColor;
    }

    void ISelectHandler.OnSelect(BaseEventData eventData)
    {
        if(!_button.interactable)
            return;
            OnSelect?.Invoke(_index);//通知选择
    }
}