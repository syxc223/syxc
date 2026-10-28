using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private bool useLocalStateInEditor = true;
    [SerializeField] private GameNetworkManager networkManager;
    [SerializeField] private NetworkStateSyncStub networkStateSyncStub;

    private INetworkStateSync _activeStateSync;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeStateSync();
    }

    private void InitializeStateSync()
    {
        if (useLocalStateInEditor || networkStateSyncStub == null)
            _activeStateSync = new LocalStateSync();
        else
            _activeStateSync = networkStateSyncStub;

        if (PuzzleProgressService.Instance != null)
            PuzzleProgressService.Instance.SetStateSync(_activeStateSync);

        if (DialogueService.Instance != null)
            DialogueService.Instance.SetStateSync(_activeStateSync);
    }

    public void StartHost()
    {
        if (networkManager == null)
        {
            Debug.LogWarning("[NetworkBootstrap] 未配置 GameNetworkManager。");
            return;
        }

        networkManager.StartHost();
    }

    public void StartClient()
    {
        if (networkManager == null)
        {
            Debug.LogWarning("[NetworkBootstrap] 未配置 GameNetworkManager。");
            return;
        }

        networkManager.StartClient();
    }

    public void StopNetwork()
    {
        if (networkManager == null)
            return;

        if (networkManager.IsHostActive || networkManager.IsClientActive)
            networkManager.StopHost();
    }
}
