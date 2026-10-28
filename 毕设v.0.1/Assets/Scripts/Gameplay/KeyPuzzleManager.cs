using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

/// <summary>
/// Something the local player can interact with using the E key in the key puzzle room.
/// Plain MonoBehaviours register here; all networking goes through KeyPuzzleManager.
/// </summary>
public interface IRoomInteractable
{
    int TargetId { get; }
    Transform InteractTransform { get; }
    float InteractRange { get; }
    string DisplayName { get; }
    string InteractVerb { get; }
    bool IsAvailable { get; }
    /// <summary>Unique search progress key; empty = does not count as a search.</summary>
    string SearchKey { get; }
    /// <summary>Server: returns the current line and advances (last line repeats).</summary>
    string AdvanceLine();
    /// <summary>Client: displays the investigation result locally.</summary>
    void ShowInteractResult(ulong senderId, string line);
    void RequestInteract();
}

/// <summary>
/// Level 1 "missing key" puzzle hub (the scene's only NetworkObject for the puzzle).
/// - Central E-key interaction: finds the nearest IRoomInteractable and shows a
///   floating world-space prompt above it.
/// - Co-op: targets are assigned to one player only (split searching); "key clue"
///   targets show their lines to the finder only, so the answer must be told aloud.
/// - The wall hint appears once the team has searched enough targets
///   (_searchesToReveal, 0 = all required targets).
/// - The door can open with a single press (demo-friendly default); the "both players
///   push within 3s" variant is kept behind _requireBothPlayersToPush.
/// - Owns the door's opened state (server-authoritative).
/// </summary>
public class KeyPuzzleManager : NetworkBehaviour
{
    public static KeyPuzzleManager Instance { get; private set; }

    /// <summary>All live interactables in the current scene (they register themselves).</summary>
    public static readonly List<IRoomInteractable> Registry = new List<IRoomInteractable>();
    /// <summary>Id → interactable routing for RPCs (ids are serialized on the targets).</summary>
    public static readonly Dictionary<int, IRoomInteractable> ById = new Dictionary<int, IRoomInteractable>();

    [Header("Wall Hint")]
    [Tooltip("墙上文字对象（谜底提示，调查完全部目标后激活）")]
    [SerializeField] GameObject _wallHintText;

    [Header("Door")]
    [SerializeField] KeyPuzzleDoor _door;

    [Header("Interact Hint (world-space TMP)")]
    [SerializeField] TextMeshPro _interactHint;

    [Header("Demo Tuning")]
    [Tooltip("合计调查多少个目标后揭示墙上金字（0 = 需要查完全部必查目标）。演示时可调小加快节奏。")]
    [SerializeField] int _searchesToReveal = 4;
    [Tooltip("是否要求两名玩家在 3 秒窗口内各按一次 E 才推得开大门（单机演示建议关闭）。")]
    [SerializeField] bool _requireBothPlayersToPush = false;

    NetworkVariable<bool> _hintRevealed = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<bool> _doorOpened = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server-only bookkeeping
    readonly HashSet<string> _searchedKeys = new HashSet<string>();
    int _doorTryCount;

    // 双人推门（仅 _requireBothPlayersToPush 开启时使用）：第一人抵门后，第二人须在窗口内跟上
    const float DOOR_PRESS_WINDOW = 3f;
    ulong _doorPresserId = ulong.MaxValue;
    float _doorPressTime;

    Transform _localPlayer;
    bool _networkReady;

    public bool HintRevealed => _hintRevealed.Value;
    public bool DoorOpened => _doorOpened.Value;
    public int SearchedCount => _searchedKeys.Count;

    /// <summary>Required search targets currently registered in the scene.</summary>
    public static int RequiredTargetCount()
    {
        int n = 0;
        foreach (var t in Registry)
            if (t is InvestigationTarget inv && inv.IsRequiredForHint)
                n++;
        return n;
    }

    void Awake()
    {
        Instance = this;
        if (_interactHint != null)
            _interactHint.gameObject.SetActive(false);
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _networkReady = true;
        _hintRevealed.OnValueChanged += OnHintRevealedChanged;
        _doorOpened.OnValueChanged += OnDoorOpenedChanged;
        ApplyHintVisual();
        if (IsServer)
        {
            _searchedKeys.Clear();
            _doorTryCount = 0;
        }
    }

    public override void OnNetworkDespawn()
    {
        _networkReady = false;
        _hintRevealed.OnValueChanged -= OnHintRevealedChanged;
        _doorOpened.OnValueChanged -= OnDoorOpenedChanged;
    }

    // ---------------- interaction requests (client → server) ----------------

    public void RequestInvestigate(int targetId)
    {
        InvestigateServerRpc(targetId);
    }

    [ServerRpc(RequireOwnership = false)]
    void InvestigateServerRpc(int targetId, ServerRpcParams rpcParams = default)
    {
        if (!ById.TryGetValue(targetId, out var target)) return;
        if (!ValidateSender(target, rpcParams)) return;

        ulong sender = rpcParams.Receive.SenderClientId;

        // 协作分工：这个目标只有它的归属玩家看得懂。读错对象 → 引导语（只发给本人），
        // 不推进台词、不计入搜索进度。
        var inv = target as InvestigationTarget;
        if (inv != null && !inv.CanBeReadBy((int)sender))
        {
            ShowInvestigationClientRpc(targetId, sender,
                "\u4F60\u76EF\u7740\u770B\u4E86\u534A\u5929\uFF0C\u8FD8\u662F\u770B\u4E0D\u51FA\u540D\u5802\u3002\u8FD9\u4E9B\u4E1C\u897F\u2014\u2014\u4E5F\u8BB8\u8BE5\u8BA9\u540C\u4F34\u6765\u770B\uFF1F",
                true, TargetedAt(sender));
            return;
        }

        string line = target.AdvanceLine();
        if (!string.IsNullOrEmpty(target.SearchKey))
            RegisterSearch(target.SearchKey);

        // 私有关键线索：只显示给发现者本人，另一个人必须听他转述
        bool privateOnly = inv != null && inv.PrivateLines;
        ShowInvestigationClientRpc(targetId, sender, line, privateOnly,
            privateOnly ? TargetedAt(sender) : default);
    }

    /// <summary>构造只发给某个客户端的 RPC 参数。</summary>
    static ClientRpcParams TargetedAt(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }

    public void RequestDoorInteract(int doorId)
    {
        DoorInteractServerRpc(doorId);
    }

    [ServerRpc(RequireOwnership = false)]
    void DoorInteractServerRpc(int doorId, ServerRpcParams rpcParams = default)
    {
        if (_door == null || _door.TargetId != doorId) return;
        if (!ValidateSender(_door, rpcParams)) return;

        HandleDoorPress(rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Server: 双人推门状态机。两人在 DOOR_PRESS_WINDOW 秒内各按一次 E 才推得开；
    /// 单独成方法（与 RPC 解耦）便于直连测试与复用。
    /// </summary>
    void HandleDoorPress(ulong sender)
    {
        if (_doorOpened.Value)
        {
            ShowDoorClientRpc(sender, _door.AlreadyOpenLine);
            return;
        }

        if (!_hintRevealed.Value)
        {
            // 金字未出：门"还锁着"，照旧给递进的锁门台词
            int idx = Mathf.Clamp(_doorTryCount, 0, _door.LockedLineCount - 1);
            if (_doorTryCount < _door.LockedLineCount - 1)
                _doorTryCount++;
            ShowDoorClientRpc(sender, _door.GetLockedLine(idx));
            return;
        }

        if (!_requireBothPlayersToPush)
        {
            // 演示模式（默认）：单人即可推门，保留揭示演出
            _doorOpened.Value = true;
            ShowDoorClientRpc(sender, _door.RevealLine);
            return;
        }

        if (_doorPresserId == ulong.MaxValue)
        {
            // 第一人：抵住门，等同伴
            _doorPresserId = sender;
            _doorPressTime = Time.time;
            ShowDoorClientRpc(sender, "\u4F60\u62BC\u4F4F\u4E86\u5927\u95E8\u2014\u2014\u5FEB\u53EB\u540C\u4F34\u4E00\u8D77\u7528\u529B\u63A8\uFF01");
            AnnounceDoorHoldClientRpc(sender);
            return;
        }

        if (Time.time - _doorPressTime > DOOR_PRESS_WINDOW)
        {
            // 上一轮窗口已过期：把新来的人当作新的抵门者
            _doorPresserId = sender;
            _doorPressTime = Time.time;
            ShowDoorClientRpc(sender, "\u521A\u624D\u6CA1\u80FD\u540C\u65F6\u7528\u529B\u3002\u4F60\u91CD\u65B0\u62BC\u4F4F\u4E86\u5927\u95E8\u2014\u2014\u5FEB\u53EB\u540C\u4F34\u4E00\u8D77\u63A8\uFF01");
            AnnounceDoorHoldClientRpc(sender);
            return;
        }

        if (_doorPresserId == sender)
        {
            // 同一个人重复按：推不动
            ShowDoorClientRpc(sender, "\u4F60\u4E00\u4E2A\u4EBA\u63A8\u4E0D\u52A8\u8FD9\u6247\u95E8\u2014\u2014\u7B49\u540C\u4F34\u4E00\u8D77\u7528\u529B\uFF01");
            return;
        }

        // 第二人到位：一起推门 → 揭示演出（全员可见）
        _doorPresserId = ulong.MaxValue;
        _doorOpened.Value = true;
        ShowDoorClientRpc(sender, _door.RevealLine);
    }

    bool ValidateSender(IRoomInteractable target, ServerRpcParams rpcParams)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(sender);
        // 找不到玩家对象时放行（直连场景下客户端必有玩家对象；此分支只对
        // 编辑器单机直接调用 RPC 的测试生效，跳过的是距离软校验，不是归属校验）
        if (playerObj == null) return true;
        // Soft server-side validation (range doubled to tolerate latency)
        return Vector3.Distance(playerObj.transform.position,
            target.InteractTransform.position) <= target.InteractRange * 2f;
    }

    [ClientRpc]
    void ShowInvestigationClientRpc(int targetId, ulong senderId, string line,
        bool privateOnly = false, ClientRpcParams rpcParams = default)
    {
        // privateOnly 时本 RPC 只会送达 senderId 所在的客户端（参数由服务器端构造）
        if (ById.TryGetValue(targetId, out var target))
            target.ShowInteractResult(senderId, line);
    }

    [ClientRpc]
    void ShowDoorClientRpc(ulong senderId, string line)
    {
        WorldDialogueBox.Show($"P{senderId + 1}", line, _door != null ? _door.ToastDuration : 5f);
        if (_door != null && !_doorOpened.Value)
            AudioManager.Instance?.PlaySfx("CubeDrop"); // dull knock on the locked door
    }

    [ClientRpc]
    void AnnounceDoorHoldClientRpc(ulong holderId)
    {
        GameHUD.Instance?.ShowToast($"P{holderId + 1} \u62BC\u4F4F\u4E86\u5927\u95E8\u2014\u2014{DOOR_PRESS_WINDOW:0} \u79D2\u5185\u4E00\u8D77\u7528\u529B\u63A8\uFF01", 3f);
        AudioManager.Instance?.PlaySfx("CubeDrop");
    }

    // ---------------- puzzle state ----------------

    void RegisterSearch(string key)
    {
        if (string.IsNullOrEmpty(key) || !_searchedKeys.Add(key)) return;

        if (!_hintRevealed.Value && _searchedKeys.Count >= SearchesNeededToReveal())
            _hintRevealed.Value = true;
    }

    /// <summary>揭示金字所需的调查数：配置为 0 时退回"查完全部必查目标"。</summary>
    int SearchesNeededToReveal()
    {
        return _searchesToReveal > 0 ? _searchesToReveal : RequiredTargetCount();
    }

    void OnHintRevealedChanged(bool oldVal, bool newVal)
    {
        ApplyHintVisual();

        if (newVal)
        {
            AudioManager.Instance?.PlaySfx("PortalEnter");
            WorldDialogueBox.Show("", "\u5C31\u5728\u8FD9\u65F6\uFF0C\u5927\u95E8\u4E0A\u65B9\u6D6E\u73B0\u51FA\u4E00\u884C\u91D1\u5B57\u2014\u2014\u300C\u94A5\u5319\u4E0D\u5728\u623F\u95F4\u91CC\u3002\u300D", 6f);
        }
    }

    void ApplyHintVisual()
    {
        if (_wallHintText != null && _wallHintText.activeSelf != _hintRevealed.Value)
            _wallHintText.SetActive(_hintRevealed.Value);
    }

    void OnDoorOpenedChanged(bool oldVal, bool newVal)
    {
        // The door itself reacts to DoorOpened in its own Update (poll pattern)
    }

    // ---------------- local E-key interaction hub ----------------

    void Update()
    {
        if (!_networkReady || ChatSystem.IsChatOpen || GameHUD.IsPaused)
        {
            HideHint();
            return;
        }

        FindLocalPlayer();
        if (_localPlayer == null)
        {
            HideHint();
            return;
        }

        // Nearest interactable wins the E key (chests sit close together)
        IRoomInteractable best = null;
        float bestDist = float.MaxValue;
        for (int i = Registry.Count - 1; i >= 0; i--)
        {
            var t = Registry[i];
            // Clean up destroyed entries (Unity null check needs the UnityEngine.Object cast)
            if (t is UnityEngine.Object uo && uo == null)
            {
                Registry.RemoveAt(i);
                continue;
            }
            if (!t.IsAvailable) continue;

            float d = Vector3.Distance(_localPlayer.position, t.InteractTransform.position);
            if (d <= t.InteractRange && d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        if (best != null)
        {
            ShowHint(best);
            if (Input.GetKeyDown(KeyCode.E))
                best.RequestInteract();
        }
        else
        {
            HideHint();
        }
    }

    void FindLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            _localPlayer = playerObj != null ? playerObj.transform : null;
        }
    }

    void ShowHint(IRoomInteractable target)
    {
        if (_interactHint == null) return;

        Vector3 pos = target.InteractTransform.position;
        float topY = pos.y + 1.6f;
        var rend = target.InteractTransform.GetComponentInChildren<Renderer>();
        if (rend != null)
            topY = Mathf.Max(topY, rend.bounds.max.y + 0.35f);

        _interactHint.transform.position = new Vector3(pos.x, topY, pos.z);

        // Billboard toward the camera so the prompt always reads
        // (TMP text is readable from its local -Z side)
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 toCam = cam.transform.position - _interactHint.transform.position;
            if (toCam.sqrMagnitude > 0.001f)
                _interactHint.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }

        _interactHint.text = $"按 E {target.InteractVerb}{target.DisplayName}";
        if (!_interactHint.gameObject.activeSelf)
            _interactHint.gameObject.SetActive(true);
    }

    void HideHint()
    {
        if (_interactHint != null && _interactHint.gameObject.activeSelf)
            _interactHint.gameObject.SetActive(false);
    }
}
