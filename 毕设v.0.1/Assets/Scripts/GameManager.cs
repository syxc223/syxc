using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages Netcode connection (Host/Client) and async scene loading.
/// Persists across scenes via DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    UnityTransport _selfTransport;

    public int CurrentRoomId { get; set; } = -1;

    Coroutine _connectTimeout;
    bool _isExiting;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _selfTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Connection succeeded — cancel timeout
            if (_connectTimeout != null)
            {
                StopCoroutine(_connectTimeout);
                _connectTimeout = null;
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx("Connect");
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        // Client-side: our own connection failed or we were disconnected
        if (!NetworkManager.Singleton.IsServer)
        {
            if (!_isExiting)
            {
                _isExiting = true;
                StartCoroutine(ReturnToLobbyAfterDisconnect());
            }
            return;
        }

        // Server-side: own shutdown goes through ExitRoom
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        if (GameHUD.Instance != null)
            GameHUD.Instance.ShowToast($"P{clientId + 1} 已断开连接", 3f);

        if (LevelManager.Instance != null && LevelManager.Instance.NetworkObject != null && LevelManager.Instance.NetworkObject.IsSpawned)
            LevelManager.Instance.OnPlayerDisconnected((int)clientId);

        // Any NPC may be waiting for P2 confirmation — cancel all pending flows.
        foreach (var npc in FindObjectsOfType<NpcDialogue>())
            if (npc != null && npc.NetworkObject != null && npc.NetworkObject.IsSpawned)
                npc.OnPlayerDisconnected(clientId);
    }

    public bool Connect(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            Debug.LogError("无效的主机 IP");
            return false;
        }

        _selfTransport.SetConnectionData(ip.Trim(), 7777);
        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("StartClient 失败，无法启动网络客户端");
            return false;
        }
        RegisterSceneEvents();

        // Start connection timeout
        if (_connectTimeout != null) StopCoroutine(_connectTimeout);
        _connectTimeout = StartCoroutine(ConnectTimeout());
        return true;
    }

    public bool StartHost()
    {
        _selfTransport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogError("StartHost 失败，端口 7777 可能被占用或被防火墙拦截");
            return false;
        }
        RegisterSceneEvents();
        StartCoroutine(LoadGameScene());
        return true;
    }

    bool _sceneEventsRegistered;

    void RegisterSceneEvents()
    {
        if (_sceneEventsRegistered) return;
        _sceneEventsRegistered = true;

        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
    }

    IEnumerator LoadGameScene()
    {
        LoadingScreen.Instance?.Begin("正在进入时空试炼之殿…");
        yield return new WaitForSeconds(0.2f);

        if (NetworkManager.Singleton.SceneManager != null)
        {
            var status = NetworkManager.Singleton.SceneManager.LoadScene("大厅", LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Failed to start 大厅 load: {status}");
                LoadingScreen.Instance?.Complete();
            }
        }
    }

    void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        // Dismiss the loading overlay first so the new scene fades in under it
        LoadingScreen.Instance?.Complete();

        if (sceneName == "大厅")
        {
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.gameObject.SetActive(true);
                GameHUD.Instance.HidePuzzleComplete();
                GameHUD.Instance.SetPuzzleName("");
                GameHUD.Instance.SetHint("");
            }

            ChatSystem.Instance?.ShowSystemMessage("欢迎回来！按 T 聊天，Y 查看聊天记录");

            // (Re)entering the NPC room: put both players back at the room spawns.
            // Runs once on the server (the host's own load complete).
            if (NetworkManager.Singleton.IsServer)
            {
                TeleportPlayer(0, _gameSceneSpawnP1);
                TeleportPlayer(1, _gameSceneSpawnP2);
            }
        }
        else if (sceneName.StartsWith("Level"))
        {
            // Any LevelN scene: prefer the level's own LevelManager config; fall
            // back to the title the NPC passed when starting the load.
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.gameObject.SetActive(true);
                GameHUD.Instance.SetPuzzleName(LevelManager.Instance != null
                    ? LevelManager.Instance.LevelName
                    : (string.IsNullOrEmpty(_currentLevelTitle) ? sceneName : _currentLevelTitle));
                GameHUD.Instance.SetHint(LevelManager.Instance != null
                    ? LevelManager.Instance.ObjectiveText
                    : "");
            }

            ChatSystem.Instance?.ShowSystemMessage("关卡已开始，按 T 聊天，Y 查看记录");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBgm("ChamberBGM");
        }
    }

    static readonly Vector3 _gameSceneSpawnP1 = new Vector3(-3f, 1f, 0f);
    static readonly Vector3 _gameSceneSpawnP2 = new Vector3(3f, 1f, 0f);

    void TeleportPlayer(ulong clientId, Vector3 pos)
    {
        var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        var pm = playerObj?.GetComponent<PlayerMove>();
        if (pm != null)
            pm.ServerTeleport(pos);
    }

    /// <summary>
    /// Load a level scene (called by NpcDialogue after mission confirmed).
    /// levelTitle is the display name from the NPC's config, used by the loading
    /// screen and as HUD fallback when the scene has no LevelManager.
    /// </summary>
    public void LoadLevel(string sceneName, string levelTitle = null)
    {
        _currentLevelTitle = levelTitle;
        StartCoroutine(LoadLevelScene(sceneName));
    }

    string _currentLevelTitle;

    IEnumerator LoadLevelScene(string sceneName)
    {
        string title = sceneName == "大厅"
            ? "正在返回试炼之殿…"
            : $"正在加载 {(string.IsNullOrEmpty(_currentLevelTitle) ? sceneName : _currentLevelTitle)}…";
        LoadingScreen.Instance?.Begin(title);
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton.SceneManager != null)
        {
            var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Failed to start {sceneName} load: {status}");
                LoadingScreen.Instance?.Complete();
            }
        }
    }

    /// <summary>
    /// Client connection timeout: if not connected within 15s, abort and return to lobby.
    /// </summary>
    IEnumerator ConnectTimeout()
    {
        yield return new WaitForSeconds(15f);

        _connectTimeout = null;

        if (NetworkManager.Singleton != null &&
            !NetworkManager.Singleton.IsServer &&
            !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogError("[GameManager] 连接超时，无法连接到主机（请检查防火墙/网络）");
            NetworkManager.Singleton.Shutdown();
        }

        // Shutdown may not trigger OnClientDisconnectCallback when the client
        // never actually connected, so return to lobby directly.
        if (!_isExiting)
        {
            _isExiting = true;
            StartCoroutine(ReturnToLobbyAfterDisconnect());
        }
    }

    /// <summary>
    /// Return to lobby after connection failure/disconnect.
    /// Waits one frame to avoid calling Shutdown inside a Netcode callback.
    /// </summary>
    IEnumerator ReturnToLobbyAfterDisconnect()
    {
        yield return null;

        if (_connectTimeout != null)
        {
            StopCoroutine(_connectTimeout);
            _connectTimeout = null;
        }

        ExitRoom();
        _isExiting = false;
    }

    /// <summary>
    /// Exit room: leave API, shutdown Netcode, return to lobby UI.
    /// </summary>
    public void ExitRoom()
    {
        if (CurrentRoomId > 0)
        {
            RoomApi.LeaveRoom(CurrentRoomId, _ => { });
            CurrentRoomId = -1;
        }

        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        _sceneEventsRegistered = false;

        NetworkManager.Singleton.Shutdown();

        // Show lobby UI
        if (UIManager.Instance != null)
            UIManager.Instance.ShowLobby();

        // Hide HUD
        if (GameHUD.Instance != null)
            GameHUD.Instance.gameObject.SetActive(false);

        // Play menu BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBgm("MenuBGM");
    }

    public void Disconnect()
    {
        ExitRoom();
    }
}
