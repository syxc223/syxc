using UnityEngine;

/// <summary>
/// 联机状态同步占位实现。安装 Mirror 后在此接入 NetworkBehaviour、[Command]、[ClientRpc]。
/// </summary>
public class NetworkStateSyncStub : MonoBehaviour, INetworkStateSync
{
    public void SyncPuzzleState(string puzzleId, bool solved)
    {
        Debug.Log($"[NetworkStateSyncStub] Puzzle sync placeholder: {puzzleId} = {solved}");
    }

    public void SyncClueState(string clueId, bool collected)
    {
        Debug.Log($"[NetworkStateSyncStub] Clue sync placeholder: {clueId} = {collected}");
    }

    public void SyncDialogueFlag(string flagId, bool value)
    {
        Debug.Log($"[NetworkStateSyncStub] Flag sync placeholder: {flagId} = {value}");
    }
}
