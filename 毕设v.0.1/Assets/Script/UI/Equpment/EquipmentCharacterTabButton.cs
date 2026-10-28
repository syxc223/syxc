using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Game;

using UnityEngine;
public class EquipmentCharacterTabButton : MonoBehaviour, ISelectHandler, ISubmitHandler
{
    // 可在Unity编辑器面板拖拽赋值的引用
    [SerializeField] private Image portraitImage;
    [SerializeField] private CanvasGroup canvasGroup;

    // 内部私有字段
    private Button _button;
    private int _index;
    private Action<int> _onSelect;   // 选中时的回调
    private Action<int> _onClicked;  // 点击时的回调

    // 只读属性，对外安全暴露Button组件
    public Button Button => _button;

    private void Awake()
    {
        // 获取Button组件并添加点击事件监听
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
        SetSelectedVisual(false); // 默认未选中状态
        
    }

    public void Setup(CharacterRuntimeData member, int index, Action<int> onSelect, Action<int> onClicked)
{
    // 记录当前页签的下标序号
    _index = index;
    // 保存选中事件回调
    _onSelect = onSelect;
    // 保存点击事件回调
    _onClicked = onClicked;

    // 安全赋值：如果头像Image组件存在，就加载角色头像
    if (portraitImage != null && member?.Definition != null)
    {
        portraitImage.sprite = member.Definition.Portrait;
        portraitImage.raycastTarget = false;
    }
}

    public void SetSelectedVisual(bool selected)
    {
        canvasGroup.alpha = selected ? 1 : 0.5f;
    }

// 设置按钮是否可以被点击交互
    public void SetInteractable(bool interactable)
    {
        _button.interactable = interactable;
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

    public void OnSubmit(BaseEventData eventData)
    {
        HandleClick();
    }
}