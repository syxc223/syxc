using Game;
using UnityEngine;

/// <summary>
/// 矛盾推理点：对某条陈述出示正确证物以推进案件。
/// </summary>
[RequireComponent(typeof(InteractionBase))]
public class ContradictionAction : ActionBase
{
    [Header("Contradiction")]
    [SerializeField] private string targetId;
    [TextArea(3, 6)]
    [SerializeField] private string statement;
    [SerializeField] private string[] acceptedEvidenceIds;
    [TextArea(2, 3)]
    [SerializeField] private string wrongEvidenceFeedback = "不对……再想想哪里有问题。";
    [SerializeField] private string[] successFlags;
    [SerializeField] private DialogueSequenceSO successDialogue;
    [SerializeField] private bool hideAfterSuccess = true;

    public string TargetId => string.IsNullOrEmpty(targetId) ? gameObject.name : targetId;
    public string Statement => statement;
    public string[] AcceptedEvidenceIds => acceptedEvidenceIds;
    public string WrongEvidenceFeedback => wrongEvidenceFeedback;
    public string[] SuccessFlags => successFlags;
    public DialogueSequenceSO SuccessDialogue => successDialogue;

    public override bool CanExecute(AllyDefinitionSO interactor)
    {
        return !hideAfterSuccess || !IsResolved();
    }

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new EvidencePresentRequestEvent(this));
    }

    public bool IsResolved()
    {
        if (successFlags == null || successFlags.Length == 0)
            return false;

        var service = PuzzleProgressService.Instance;
        if (service == null)
            return false;

        for (int i = 0; i < successFlags.Length; i++)
        {
            if (!service.HasFlag(successFlags[i]))
                return false;
        }

        return true;
    }

    public bool TryPresentEvidence(string evidenceId, out string feedback, out DialogueSequenceSO dialogue)
    {
        feedback = wrongEvidenceFeedback;
        dialogue = null;

        if (string.IsNullOrEmpty(evidenceId) || acceptedEvidenceIds == null)
            return false;

        for (int i = 0; i < acceptedEvidenceIds.Length; i++)
        {
            if (acceptedEvidenceIds[i] != evidenceId)
                continue;

            PuzzleProgressService.Instance?.TrySetFlags(successFlags);
            feedback = "没错！矛盾被识破了！";
            dialogue = successDialogue;

            if (hideAfterSuccess)
            {
                enabled = false;
            }

            return true;
        }

        return false;
    }
}
