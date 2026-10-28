using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using System.Collections.Generic;
public class DialoguePanelController : PanelController
{
    [Header("Dialogue Panel")]
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text DifficultyText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    private DialogueAction _dialogueAction;
    private readonly System.Collections.Generic.List<Button> _spawnedChoiceButtons = new();

    public override Type PanelActionType => typeof(DialogueAction);

    public void OpenSequence(DialogueSequenceSO sequence)
    {
        if (sequence == null)
            return;

        DialogueService.Instance?.StartDialogue(sequence);
        gameObject.SetActive(true);
        FirstButton = confirmButton;
        RebindButtons(confirmButton, OnContinuePressed);
        if (cancelButton != null)
            RebindButtons(cancelButton, EndDialogue);
        RefreshView();
        SetDefaultSelection();
        GameModeManager.Instance.RequestChangeMode(GameMode.Dialogue);
    }

    public override void SetupPanel(ActionBase actionBase)
    {
        base.SetupPanel(actionBase);
        _dialogueAction = actionBase as DialogueAction;
        if (_dialogueAction == null || _dialogueAction.Sequence == null)
            return;

        DialogueService.Instance?.StartDialogue(_dialogueAction.Sequence);
        FirstButton = confirmButton;
        RebindButtons(confirmButton, OnContinuePressed);
        if (cancelButton != null)
            RebindButtons(cancelButton, EndDialogue);
        RefreshView();
        SetDefaultSelection();
        GameModeManager.Instance.RequestChangeMode(GameMode.Dialogue);
    }

    public override bool HandleCancelInput()
    {
        if (!gameObject.activeSelf)
            return false;

        EndDialogue();
        return true;
    }

    private void OnContinuePressed()
    {
        if (DialogueService.Instance == null || !DialogueService.Instance.IsPlaying)
        {
            EndDialogue();
            return;
        }

        if (!DialogueService.Instance.TryGetCurrentNode(out var node))
        {
            EndDialogue();
            return;
        }

        var choices = DialogueService.Instance.GetVisibleChoices(node);
        if (choices.Count > 0)
            return;

        DialogueService.Instance.AdvanceAuto();
        if (!DialogueService.Instance.IsPlaying)
            EndDialogue();
        else
            RefreshView();
    }

    private void RefreshView()
    {
        ClearChoices();

        if (DialogueService.Instance == null || !DialogueService.Instance.TryGetCurrentNode(out var node))
        {
            EndDialogue();
            return;
        }

        if (npcNameText != null)
            npcNameText.text = node.SpeakerName;
        if (DifficultyText != null)
            DifficultyText.text = node.Text;
        if (characterImage != null)
        {
            characterImage.sprite = node.Portrait;
            characterImage.enabled = node.Portrait != null;
        }

        var choices = DialogueService.Instance.GetVisibleChoices(node);
        if (choices.Count > 0 && choiceContainer != null && choiceButtonPrefab != null)
        {
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var button = Instantiate(choiceButtonPrefab, choiceContainer);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = choice.Text;

                button.onClick.AddListener(() => OnChoiceSelected(choice));
                _spawnedChoiceButtons.Add(button);
            }

            if (_spawnedChoiceButtons.Count > 0)
                FirstButton = _spawnedChoiceButtons[0];
            return;
        }

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        DialogueService.Instance?.SelectChoice(choice);
        if (DialogueService.Instance == null || !DialogueService.Instance.IsPlaying)
            EndDialogue();
        else
            RefreshView();
    }

    private void ClearChoices()
    {
        for (int i = 0; i < _spawnedChoiceButtons.Count; i++)
        {
            if (_spawnedChoiceButtons[i] != null)
                Destroy(_spawnedChoiceButtons[i].gameObject);
        }

        _spawnedChoiceButtons.Clear();
    }

    private void EndDialogue()
    {
        DialogueService.Instance?.EndDialogue();
        ClearChoices();
        ClosePanel();
        GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
    }
}
