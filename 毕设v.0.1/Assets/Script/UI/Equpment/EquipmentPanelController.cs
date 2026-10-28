using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game;
using TMPro;

public class EquipmentPanelController : PanelController
{
    private enum FocusLayer
    {
        CharacterTabs,
        SlotList,
        CandidateList
    }
    private enum EquipmentUiLayer
    {
        CharacterTabs,
        Slots
    }

    [Header("Character Tabs")]
    [SerializeField] private RectTransform tabRoot;
    [SerializeField] private CanvasGroup tabCanvasGroup;
    [SerializeField] private EquipmentCharacterTabButton tabPrefab;

    [Header("Slot Buttons")]
    [SerializeField] private EquipmentSlotButton[] slotButtons;
    [SerializeField] private CanvasGroup slotListCanvasGroup;

    [Header("Left Category Name")]
    [SerializeField] private TMP_Text[] leftCategoryNameTexts;
    [SerializeField] private Color leftCategoryEnabledColor = Color.black;
    [SerializeField] private Color leftCategoryDisabledColor = new Color(0.72f, 0.72f, 0.72f);

    [Header("Header")]
    [SerializeField] private TMP_Text memberNameText;

    [Header("Candidate Panel")]
    [SerializeField] private GameObject candidatePanelRoot;
    [SerializeField] private EquipmentCandidatePanelController candidatePanelController;

    [Header("Stat Preview")]
    [SerializeField] private EquipmentStatPreviewPanel statPreviewPanel;

    [Header("Overlay")]
    [SerializeField] private RectTransform confirmPopup;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    #region 固定槽位顺序

    private static readonly WeaponType[] WeaponSlotTypes =
    {
        WeaponType.Sword,
        WeaponType.Spear,
        WeaponType.Dagger,
        WeaponType.Axe,
        WeaponType.Bow,
        WeaponType.Staff
    };

    private static readonly EquipSlot[] FixedSlotOrder =
    {
        EquipSlot.Sword,
        EquipSlot.Spear,
        EquipSlot.Dagger,
        EquipSlot.Axe,
        EquipSlot.Bow,
        EquipSlot.Staff,
        EquipSlot.Shield,
        EquipSlot.Head,
        EquipSlot.Body,
        EquipSlot.Accessory1,
        EquipSlot.Accessory2
    };

    #endregion

    #region 运行时缓存

    private readonly List<EquipmentCharacterTabButton> _tabButtons = new();
    private readonly List<CharacterRuntimeData> _partyMembers = new();
    private int _memberIndex;
    private int _slotIndex;

    private FocusLayer _currentFocus = FocusLayer.CharacterTabs;
    private EquipmentUiLayer _currentLayer = EquipmentUiLayer.CharacterTabs;

    private CharacterRuntimeData CurrentMember =>
        _memberIndex >= 0 && _memberIndex < _partyMembers.Count ? _partyMembers[_memberIndex] : null;

    #endregion

    private void Update()
    {
        if (!isActiveAndEnabled || _currentLayer != EquipmentUiLayer.CharacterTabs || _tabButtons.Count <= 1)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
            return;

        int delta = keyboard.shiftKey.isPressed ? -1 : 1;
        SelectAdjacentMember(delta);
    }

    public void OnPanelOpened()
    {
        if (confirmPopup != null)
            confirmPopup.gameObject.SetActive(false);

        if (candidatePanelRoot != null)
            candidatePanelRoot.SetActive(false);

        _currentFocus = FocusLayer.CharacterTabs;
        SetOverlayMode(false);
        RestoreDefaultSelection();
    }

    public void SetOverlayMode(bool overlayActive)
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = !overlayActive;
            panelCanvasGroup.blocksRaycasts = !overlayActive;
        }

        if (overlayActive)
            return;

        ApplyLayerInteraction();
    }

    public void OpenConfirmPopup()
    {
        if (confirmPopup == null)
            return;

        confirmPopup.gameObject.SetActive(true);
        SetOverlayMode(true);
    }

    public void CloseConfirmPopup()
    {
        if (confirmPopup == null)
            return;

        confirmPopup.gameObject.SetActive(false);
        SetOverlayMode(false);
        RestoreDefaultSelection();
    }

    public void RestoreDefaultSelection()
    {
        if (FirstButton != null)
            SetDefaultSelection();
    }

    public override bool HandleCancelInput()
    {
        if (confirmPopup != null && confirmPopup.gameObject.activeSelf)
        {
            CloseConfirmPopup();
            return true;
        }

        if (candidatePanelRoot != null && candidatePanelRoot.activeInHierarchy)
        {
            CloseCandidateList();
            return true;
        }

        if (_currentLayer == EquipmentUiLayer.Slots)
        {
            EnterCharacterTabLayer();
            return true;
        }

        return false;
    }

    public void SetupWithPartyMember(List<CharacterRuntimeData> partyMembers)
    {
        _partyMembers.Clear();
        if (partyMembers == null)
            return;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            if (partyMembers[i] == null)
                continue;

            _partyMembers.Add(partyMembers[i]);
        }

        BuildCharacterTabs();
        ClampMemberIndex();
        EnterCharacterTabLayer();
    }

    /// <summary>第一层：只操作角色 Tab（对应商店的「购买/出售」层）。</summary>
    private void EnterCharacterTabLayer()
    {
        _currentLayer = EquipmentUiLayer.CharacterTabs;
        _currentFocus = FocusLayer.CharacterTabs;

        SetCharacterTabInteractable(true);
        UpdateTabSelectionVisual();
        SetSlotListInteractable(false);
        RefreshMemberDisplayForTabLayer();

        if (_tabButtons.Count == 0)
            return;

        FirstButton = _tabButtons[_memberIndex].Button;
        SetDefaultSelection();
    }

    /// <summary>第二层：确认角色后操作装备槽（对应商店的「商品列表」层）。</summary>
    private void EnterSlotLayer()
    {
        if (candidatePanelRoot != null && candidatePanelRoot.activeInHierarchy)
            CloseCandidateListInternal();

        if (CurrentMember == null || slotButtons == null || slotButtons.Length == 0)
            return;

        _currentLayer = EquipmentUiLayer.Slots;
        _currentFocus = FocusLayer.SlotList;

        RefreshCurrentMemberView();
        SetSlotListInteractable(true);
        SetCharacterTabInteractable(false);
        EnsureSelectedSlotValid();

        if (_slotIndex < 0 || _slotIndex >= slotButtons.Length)
            return;

        FirstButton = slotButtons[_slotIndex].Button;
        SetDefaultSelection();
    }

    private void SetCharacterTabInteractable(bool interactable)
    {
        if (tabCanvasGroup != null)
        {
            tabCanvasGroup.interactable = interactable;
            tabCanvasGroup.blocksRaycasts = interactable;
        }

        for (int i = 0; i < _tabButtons.Count; i++)
            _tabButtons[i].SetInteractable(interactable);
    }

    private void EnsureSelectedSlotValid()
    {
        if (IsSlotIndexUsable(_slotIndex))
            return;

        for (int i = 0; i < FixedSlotOrder.Length; i++)
        {
            if (IsSlotIndexUsable(i))
            {
                _slotIndex = i;
                return;
            }
        }

        _slotIndex = -1;
    }

    private bool IsSlotIndexUsable(int index) =>
        index >= 0 && index < FixedSlotOrder.Length && IsSlotUsableForMember(CurrentMember, index);

    private void SetSlotListInteractable(bool interactable)
    {
        if (slotListCanvasGroup != null)
        {
            slotListCanvasGroup.interactable = interactable;
            slotListCanvasGroup.blocksRaycasts = interactable;
        }

        if (slotButtons == null)
            return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
                slotButtons[i].SetInputEnabled(interactable);
        }
    }

    private void ApplyLayerInteraction()
    {
        if (_currentLayer == EquipmentUiLayer.CharacterTabs)
            EnterCharacterTabLayer();
        else
            EnterSlotLayer();
    }

    private void BuildCharacterTabs()
    {
        ClearTabButtons();

        for (int i = 0; i < _partyMembers.Count; i++)
        {
            var tab = Instantiate(tabPrefab, tabRoot);
            tab.Setup(_partyMembers[i], i, OnTabSelected, OnTabClick);
            tab.SetSelectedVisual(i == _memberIndex);
            _tabButtons.Add(tab);
        }

        ConfigureTabNavigation();
    }

    private void ConfigureTabNavigation()
    {
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            Button button = _tabButtons[i].Button;
            if (button == null)
                continue;

            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = i > 0 ? _tabButtons[i - 1].Button : null;
            nav.selectOnRight = i < _tabButtons.Count - 1 ? _tabButtons[i + 1].Button : null;
            button.navigation = nav;
        }
    }

    private void SelectAdjacentMember(int delta)
    {
        if (_partyMembers.Count == 0)
            return;

        int next = (_memberIndex + delta + _partyMembers.Count) % _partyMembers.Count;
        OnTabSelected(next);
        FocusTabButton(next);
    }

    private void FocusTabButton(int index)
    {
        if (index < 0 || index >= _tabButtons.Count)
            return;

        Button tabButton = _tabButtons[index].Button;
        if (tabButton == null || !tabButton.interactable)
            return;

        EventSystem.current.SetSelectedGameObject(tabButton.gameObject);
    }

    private void RefreshCurrentMemberView()
    {
        if (memberNameText != null)
            memberNameText.text = CurrentMember?.DisplayName ?? string.Empty;

        if (slotButtons == null || CurrentMember == null)
            return;

        for (int i = 0; i < FixedSlotOrder.Length && i < slotButtons.Length; i++)
        {
            EquipmentSlotButton slotButton = slotButtons[i];
            if (slotButton == null)
                continue;

            EquipSlot slot = FixedSlotOrder[i];
            bool usable = IsSlotUsableForMember(CurrentMember, i);
            EquipmentItemSO equippedItem = CurrentMember.GetEquippedItem(slot);
            slotButton.Setup(equippedItem, i, OnSlotSelected, OnSlotClicked, usable, inputEnabled: false);
        }

        RefreshLeftCategoryLabels();
        RefreshStatPreview();
    }

    private void RefreshLeftCategoryLabels()
    {
        if (leftCategoryNameTexts == null)
            return;

        for (int i = 0; i < leftCategoryNameTexts.Length && i < FixedSlotOrder.Length; i++)
        {
            if (leftCategoryNameTexts[i] == null)
                continue;

            bool usable = CurrentMember != null && IsSlotUsableForMember(CurrentMember, i);
            leftCategoryNameTexts[i].color = usable ? leftCategoryEnabledColor : leftCategoryDisabledColor;
        }
    }

    private void RefreshStatPreview()
    {
        if (statPreviewPanel == null || CurrentMember == null)
            return;

        StatBlock total = CurrentMember.GetTotalStats();
        statPreviewPanel.Refresh(total, total, isInPreviewMode: false);
    }

    /// <summary>Tab 层只读刷新：更新槽位/属性展示，但不允许操作槽位。</summary>
    private void RefreshMemberDisplayForTabLayer()
    {
        RefreshCurrentMemberView();
        SetSlotListInteractable(false);
    }

    private void UpdateTabSelectionVisual()
    {
        for (int i = 0; i < _tabButtons.Count; i++)
            _tabButtons[i].SetSelectedVisual(i == _memberIndex);
    }

    private bool IsSlotUsableForMember(CharacterRuntimeData member, int slotIndex)
    {
        if (member == null)
            return false;

        if (slotIndex >= WeaponSlotTypes.Length)
            return true;

        return member.Definition is AllyDefinitionSO allyDef
            && allyDef.CanEquipWeaponType(WeaponSlotTypes[slotIndex]);
    }

    /// <summary>方向键/手柄移动：更新 Tab 高亮，并只读刷新该角色装备展示，不进入槽位层。</summary>
    private void OnTabSelected(int index)
    {
        if (_partyMembers.Count == 0)
            return;

        _memberIndex = Mathf.Clamp(index, 0, _partyMembers.Count - 1);
        UpdateTabSelectionVisual();
        RefreshMemberDisplayForTabLayer();
    }

    /// <summary>鼠标点击 / 确认键：选定角色并进入装备槽层。</summary>
    private void OnTabClick(int index)
    {
        OnTabSelected(index);
        EnterSlotLayer();
    }

    #region 装备槽回调

    private void OnSlotSelected(int index)
    {
        if (!IsSlotIndexUsable(index))
            return;

        _slotIndex = index;

        if (CurrentMember == null || statPreviewPanel == null)
            return;

        StatBlock total = CurrentMember.GetTotalStats();
        statPreviewPanel.Refresh(total, total, isInPreviewMode: false);
    }

    private void OnSlotClicked(int index)
    {
        OnSlotSelected(index);
        OpenCandidateList(index);
    }

    #endregion

    #region 可选择装备面板

    private void OpenCandidateList(int slotIndex)
    {
        if (candidatePanelRoot == null || candidatePanelController == null)
            return;

        SetSlotListInteractable(false);
        SetCharacterTabInteractable(false);
        _currentFocus = FocusLayer.CandidateList;
        candidatePanelRoot.SetActive(true);

        EquipSlot slot = FixedSlotOrder[slotIndex];
        string slotName = leftCategoryNameTexts != null && slotIndex < leftCategoryNameTexts.Length
            && leftCategoryNameTexts[slotIndex] != null
            ? leftCategoryNameTexts[slotIndex].text
            : slot.ToString();
        Sprite slotIcon = slotButtons != null && slotIndex < slotButtons.Length
            ? slotButtons[slotIndex].SlotIconSprite
            : null;

        candidatePanelController.OpenForSlot(slot, CurrentMember, slotName, slotIcon);
        SetSlotListInteractable(false);
    }

    private void CloseCandidateList()
    {
        CloseCandidateListInternal();
        EnterSlotLayer();
    }

    #endregion

    private void ClearTabButtons()
    {
        for (int i = 0; i < _tabButtons.Count; i++)
            Destroy(_tabButtons[i].gameObject);

        _tabButtons.Clear();
    }

    private void ClampMemberIndex()
    {
        _memberIndex = _partyMembers.Count > 0
            ? Mathf.Clamp(_memberIndex, 0, _partyMembers.Count - 1)
            : 0;
    }

    private void CloseCandidateListInternal()
    {
        if (candidatePanelController != null)
            candidatePanelController.Close();

        if (candidatePanelRoot != null)
            candidatePanelRoot.SetActive(false);
    }
}
