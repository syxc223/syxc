using UnityEngine;

/// <summary>
/// 联机管理器占位。安装 Mirror 后可改为继承 Mirror.NetworkManager。
/// </summary>
public class GameNetworkManager : MonoBehaviour
{
    public bool IsHostActive { get; private set; }
    public bool IsClientActive { get; private set; }

    public void StartHost()
    {
        IsHostActive = true;
        IsClientActive = true;
        Debug.Log("[GameNetworkManager] StartHost 占位调用。安装 Mirror 后替换为 NetworkManager.StartHost()。");
    }

    public void StartClient()
    {
        IsClientActive = true;
        Debug.Log("[GameNetworkManager] StartClient 占位调用。安装 Mirror 后替换为 NetworkManager.StartClient()。");
    }

    public void StopHost()
    {
        IsHostActive = false;
        IsClientActive = false;
        Debug.Log("[GameNetworkManager] StopHost 占位调用。");
    }
}
