using System;
using System.Collections.Generic;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentCandidatePanelController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text slotNameText;
    [SerializeField] private Image slotIconImage;

    [Header("List")]
    [SerializeField] private RectTransform candidateListRoot;
    [SerializeField] private EquipmentCandidateButton candidateButtonPrefab;

    private readonly List<EquipmentCandidateButton> _buttons = new();
    private readonly List<EquipmentItemSO> _candidates = new();


    public void OpenForSlot(EquipSlot slot, CharacterRuntimeData member, string slotDisplayName, Sprite slotIcon)
    {
        RefreshHeader(slotDisplayName, slotIcon);
        RebuildCandidates(slot);
    }

    private void RebuildCandidates(EquipSlot slot)
    {
        _candidates.Clear();
        ClearButtons();

        InventoryManager inventory = InventoryManager.Instance;
        PartyManager partyManager = PartyManager.Instance;

        _candidates.AddRange(EquipmentService.BuildCandidates(inventory, slot));

        for (int i = 0; i < _candidates.Count; i++)
    {
        int availableCount = EquipmentService.GetAvailableItemCount(inventory, partyManager, _candidates[i]);
        EquipmentCandidateButton candidateButton = Instantiate(candidateButtonPrefab, candidateListRoot);

        bool isInteractable = _candidates[i] == null || availableCount > 0;
        candidateButton.Setup(i, _candidates[i], availableCount, isInteractable, inventory.IconSet);
        _buttons.Add(candidateButton);
    }
    }

    private void RefreshHeader(string slotDisplayName, Sprite slotIcon)
    {
        slotNameText.text = slotDisplayName;
        slotIconImage.sprite = slotIcon;
        slotIconImage.enabled = slotIcon != null;
    }

    public void Close()
    {
        _candidates.Clear();
        ClearButtons();
    }

    private void ClearButtons()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)

                Destroy(_buttons[i].gameObject);
        }
        _buttons.Clear();
    }
}