using MFramework.Event;

public readonly struct PuzzleSolvedEvent : IEvent
{
    public readonly string PuzzleId;

    public PuzzleSolvedEvent(string puzzleId)
    {
        PuzzleId = puzzleId;
    }
}

public readonly struct ClueCollectedEvent : IEvent
{
    public readonly string ClueId;

    public ClueCollectedEvent(string clueId)
    {
        ClueId = clueId;
    }
}

public readonly struct PuzzleProgressChangedEvent : IEvent
{
    public readonly string ChangedId;
    public readonly PuzzleProgressChangeType ChangeType;

    public PuzzleProgressChangedEvent(string changedId, PuzzleProgressChangeType changeType)
    {
        ChangedId = changedId;
        ChangeType = changeType;
    }
}

public enum PuzzleProgressChangeType
{
    ClueCollected,
    PuzzleSolved,
    FlagSet,
    SaveLoaded
}
