using Game;

using UnityEngine;
public class DialogueAction : ActionBase
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSequenceSO dialogueSequence;

    public DialogueSequenceSO Sequence => dialogueSequence;

    public void Configure(DialogueSequenceSO sequence)
    {
        dialogueSequence = sequence;
    }

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new PanelRequestEvent(this));
    }
}
