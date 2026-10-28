using MFramework.Event;

public readonly struct EvidenceCollectedEvent : IEvent
{
    public readonly EvidenceDefinitionSO Evidence;

    public EvidenceCollectedEvent(EvidenceDefinitionSO evidence)
    {
        Evidence = evidence;
    }
}

public readonly struct EvidencePresentedEvent : IEvent
{
    public readonly string TargetId;
    public readonly string EvidenceId;
    public readonly bool Accepted;

    public EvidencePresentedEvent(string targetId, string evidenceId, bool accepted)
    {
        TargetId = targetId;
        EvidenceId = evidenceId;
        Accepted = accepted;
    }
}

public readonly struct InvestigationPhaseChangedEvent : IEvent
{
    public readonly InvestigationPhase NewPhase;

    public InvestigationPhaseChangedEvent(InvestigationPhase newPhase)
    {
        NewPhase = newPhase;
    }
}

public readonly struct EvidencePresentRequestEvent : IEvent
{
    public readonly string TargetId;
    public readonly string PromptText;
    public readonly string[] AcceptedEvidenceIds;
    public readonly string WrongFeedback;
    public readonly string[] SuccessFlags;
    public readonly DialogueSequenceSO SuccessDialogue;
    public readonly PresentEvidenceAction PresentAction;
    public readonly ContradictionAction ContradictionAction;

    public EvidencePresentRequestEvent(PresentEvidenceAction action)
    {
        PresentAction = action;
        ContradictionAction = null;
        TargetId = action.TargetId;
        PromptText = action.PromptText;
        AcceptedEvidenceIds = null;
        WrongFeedback = action.DefaultRejectText;
        SuccessFlags = null;
        SuccessDialogue = null;
    }

    public EvidencePresentRequestEvent(ContradictionAction action)
    {
        PresentAction = null;
        ContradictionAction = action;
        TargetId = action.TargetId;
        PromptText = action.Statement;
        AcceptedEvidenceIds = action.AcceptedEvidenceIds;
        WrongFeedback = action.WrongEvidenceFeedback;
        SuccessFlags = action.SuccessFlags;
        SuccessDialogue = action.SuccessDialogue;
    }
}
