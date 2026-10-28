public interface INetworkStateSync
{
    void SyncPuzzleState(string puzzleId, bool solved);
    void SyncClueState(string clueId, bool collected);
    void SyncDialogueFlag(string flagId, bool value);
}
