public class LocalStateSync : INetworkStateSync
{
    public void SyncPuzzleState(string puzzleId, bool solved)
    {
        // 单机模式：PuzzleProgressService 已在本地写入，无需额外同步。
    }

    public void SyncClueState(string clueId, bool collected)
    {
        // 单机模式：线索状态已在本地写入。
    }

    public void SyncDialogueFlag(string flagId, bool value)
    {
        // 单机模式：对话标记已在本地写入。
    }
}
