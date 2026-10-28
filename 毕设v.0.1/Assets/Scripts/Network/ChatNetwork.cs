using Unity.Netcode;

/// <summary>
/// Network relay for chat messages. Placed in gameplay scenes (GameScene, Level1).
/// Client sends message via ServerRpc → server broadcasts to all via ClientRpc,
/// tagging the message with the sender's clientId so receivers can color it.
/// </summary>
public class ChatNetwork : NetworkBehaviour
{
    public static ChatNetwork Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendChatServerRpc(string sender, string message, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        ReceiveChatClientRpc(senderId, sender, message);
    }

    [ClientRpc]
    void ReceiveChatClientRpc(ulong senderId, string sender, string message)
    {
        ChatSystem.Instance?.OnMessageReceived(senderId, sender, message);
    }
}
