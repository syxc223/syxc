using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Game;
using UnityEngine;
public class EquipmentSlotButton : MonoBehaviour, ISelectHandler
{
    [SerializeField] TMP_Text slotNameText;
    [SerializeField] Image slotIconImage;

    private CanvasGroup canvasGroup;
    private Button _button;

    private int _index;
    private Action<int> _onSelect;
    private Action<int> _onClicked;

    public Button Button
   {
    get
    {
        if(_button == null)
            {
                _button = GetComponent<Button>();
                 _defaultNavigation = _button.navigation;
        }
        return _button;
    }
   }
     private bool _isSlotUsable = true;
     private bool _isInputEnabled = false;

     public Sprite SlotIconSprite => slotIconImage.sprite;

     private Navigation _defaultNavigation;

     private void Awake()
    {
        Button.onClick.AddListener(HandleClick);
        canvasGroup = GetComponent<CanvasGroup>();
    }

   // void OnDisable()
    //{
     //   _button.onClick.RemoveListener(HandleClick);
   // }

    public void Setup(
        EquipmentItemSO equipmentItem,
        int index,
        Action<int> onSelect,
        Action<int> onClicked,
        bool isSlotUsable,
        bool inputEnabled = true)
    {
        _index = index;
        _onSelect = onSelect;
        _onClicked = onClicked;
        _isSlotUsable = isSlotUsable;
        _isInputEnabled = inputEnabled;

        string displayName = equipmentItem != null ? equipmentItem.ItemName : "未装备";
        if (slotNameText != null)
            slotNameText.text = displayName;

        ApplyButtonInteractableState();
    }

    public void SetInputEnabled(bool enabled)
    {
        _isInputEnabled = enabled;
        ApplyButtonInteractableState();
    }


    private void ApplyButtonInteractableState()
    {

    bool isInteractable = _isSlotUsable && _isInputEnabled;
    Button.interactable = isInteractable;
    canvasGroup.alpha = _isSlotUsable ? 1f : 0f;
    canvasGroup.interactable = isInteractable;

    Navigation navigation = _defaultNavigation;

    if (!isInteractable)
    {
        navigation.mode = Navigation.Mode.None;
    }

    Button.navigation = navigation;
    }


    private void HandleClick()
    {
        if(!_button.interactable)
            return;
        _onClicked?.Invoke(_index);
    }

    // ISelectHandler接口实现（EventSystem选中事件）
    public void OnSelect(BaseEventData eventData)
    {
        if(!_button.interactable)
            return;
        _onSelect?.Invoke(_index);
    }
}