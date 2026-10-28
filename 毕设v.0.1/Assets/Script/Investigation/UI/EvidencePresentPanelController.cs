using System;
using System.Collections.Generic;
using MFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvidencePresentPanelController : PanelController, IEventReceiver<EvidencePresentRequestEvent>
{
    [Header("Evidence Present")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Transform evidenceListContainer;
    [SerializeField] private Button evidenceButtonPrefab;
    [SerializeField] private Button cancelButton;

    private PresentEvidenceAction _presentAction;
    private ContradictionAction _contradictionAction;
    private readonly List<Button> _spawnedButtons = new();

    private void OnEnable()
    {
        EventBus.Subscribe<EvidencePresentRequestEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EvidencePresentRequestEvent>(this);
    }

    public void OnEvent(EvidencePresentRequestEvent evt)
    {
        _presentAction = evt.PresentAction;
        _contradictionAction = evt.ContradictionAction;

        gameObject.SetActive(true);
        if (promptText != null)
            promptText.text = evt.PromptText;
        if (feedbackText != null)
            feedbackText.text = string.Empty;

        BuildEvidenceList();
        FirstButton = cancelButton;
        RebindButtons(cancelButton, ClosePresentPanel);
        SetDefaultSelection();

        var mode = _contradictionAction != null ? GameMode.Contradiction : GameMode.EvidencePresent;
        GameModeManager.Instance?.RequestChangeMode(mode);
    }

    private void BuildEvidenceList()
    {
        ClearEvidenceButtons();

        var evidenceList = InvestigationService.Instance?.GetCollectedEvidenceDefinitions();
        if (evidenceList == null || evidenceList.Count == 0)
        {
            if (feedbackText != null)
                feedbackText.text = "还没有收集到证物，先去调查场景吧。";
            return;
        }

        if (evidenceButtonPrefab == null || evidenceListContainer == null)
            return;

        for (int i = 0; i < evidenceList.Count; i++)
        {
            var evidence = evidenceList[i];
            var button = Instantiate(evidenceButtonPrefab, evidenceListContainer);
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = evidence.ItemName;

            string evidenceId = evidence.EvidenceId;
            button.onClick.AddListener(() => OnEvidenceSelected(evidenceId));
            _spawnedButtons.Add(button);
        }

        if (_spawnedButtons.Count > 0)
            FirstButton = _spawnedButtons[0];
    }

    private void OnEvidenceSelected(string evidenceId)
    {
        bool accepted;
        string feedback;
        DialogueSequenceSO dialogue;

        if (_contradictionAction != null)
            accepted = _contradictionAction.TryPresentEvidence(evidenceId, out feedback, out dialogue);
        else if (_presentAction != null)
            accepted = _presentAction.TryPresentEvidence(evidenceId, out feedback, out dialogue);
        else
            return;

        if (feedbackText != null)
            feedbackText.text = feedback;

        EventBus.Publish(new EvidencePresentedEvent(
            _presentAction != null ? _presentAction.TargetId : _contradictionAction.TargetId,
            evidenceId,
            accepted));

        if (dialogue != null)
        {
            ClosePresentPanel();
            var dialoguePanel = FindObjectOfType<DialoguePanelController>(true);
            dialoguePanel?.OpenSequence(dialogue);
            return;
        }

        if (accepted && _contradictionAction != null)
            ClosePresentPanel();
    }

    private void ClosePresentPanel()
    {
        ClearEvidenceButtons();
        ClosePanel();
        GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
    }

    public override bool HandleCancelInput()
    {
        if (!gameObject.activeSelf)
            return false;

        ClosePresentPanel();
        return true;
    }

    private void ClearEvidenceButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                Destroy(_spawnedButtons[i].gameObject);
        }

        _spawnedButtons.Clear();
    }
}
