using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Level manager: tracks exit state, shows completion UI, returns to NPC room on finish.
/// </summary>
public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    // Per-level display config (each LevelN scene sets its own values).
    [Header("Level Info")]
    [SerializeField] string _levelName = "第一关：消失的钥匙";
    [SerializeField] string _objectiveText = "目标：大门\"锁着\"——只有找到\"正确的钥匙\"才能离开。两人一起抵达出口";

    public string LevelName => _levelName;
    public string ObjectiveText => _objectiveText;

    NetworkVariable<bool> _p1InExit = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<bool> _p2InExit = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Spawn Points")]
    [SerializeField] Vector3 _p1Spawn = new Vector3(-3f, 1f, 0f);
    [SerializeField] Vector3 _p2Spawn = new Vector3(3f, 1f, 0f);

    [Header("UI")]
    [SerializeField] GameObject _completePanel;
    [SerializeField] TMP_Text _completeText;
    [SerializeField] TMP_Text _statusText;

    bool _completeShown;

    public bool IsComplete => _p1InExit.Value && _p2InExit.Value;

    public string StatusHint =>
        $"出口：P1 {(_p1InExit.Value ? "✓" : "…")}   P2 {(_p2InExit.Value ? "✓" : "…")}　　{ObjectiveText}";

    void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        _p1InExit.OnValueChanged += OnExitChanged;
        _p2InExit.OnValueChanged += OnExitChanged;

        // Player objects persist across scene loads (their OnNetworkSpawn runs only
        // once), so the level itself must put them at their spawn points.
        if (IsServer)
        {
            TeleportPlayerToSpawn(0, _p1Spawn);
            TeleportPlayerToSpawn(1, _p2Spawn);
        }

        GameHUD.Instance?.SetPuzzleName(LevelName);
        UpdateStatus();
    }

    void TeleportPlayerToSpawn(ulong clientId, Vector3 pos)
    {
        var playerObj = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId)
            : null;
        var pm = playerObj?.GetComponent<PlayerMove>();
        if (pm != null)
            pm.ServerTeleport(pos);
    }

    public override void OnNetworkDespawn()
    {
        _p1InExit.OnValueChanged -= OnExitChanged;
        _p2InExit.OnValueChanged -= OnExitChanged;
    }

    /// <summary>
    /// Called by LevelExit when a player enters/leaves.
    /// Server only.
    /// </summary>
    public void OnPlayerInExit(int playerNumber, bool inExit)
    {
        if (!IsServer) return;

        if (playerNumber == 0)
            _p1InExit.Value = inExit;
        else
            _p2InExit.Value = inExit;
    }

    /// <summary>
    /// Called by GameManager when a client disconnects: clear their exit state
    /// (player object destruction does not trigger OnTriggerExit).
    /// </summary>
    public void OnPlayerDisconnected(int playerNumber)
    {
        if (!IsServer) return;

        if (playerNumber == 0)
            _p1InExit.Value = false;
        else
            _p2InExit.Value = false;
    }

    void OnExitChanged(bool oldVal, bool newVal)
    {
        UpdateStatus();

        if (_p1InExit.Value && _p2InExit.Value)
        {
            ShowComplete();
        }
    }

    void UpdateStatus()
    {
        if (_statusText != null)
        {
            // NOTE: TMP does not recognize the color name "gray" (only "grey"/hex),
            // which made these tags render as literal text — use hex colors.
            string p1 = _p1InExit.Value ? "<color=green>P1 ✓</color>" : "<color=#B0B0B0>P1 ...</color>";
            string p2 = _p2InExit.Value ? "<color=green>P2 ✓</color>" : "<color=#B0B0B0>P2 ...</color>";
            _statusText.text = $"{p1}  {p2}";
        }

        GameHUD.Instance?.SetHint(StatusHint);
    }

    void ShowComplete()
    {
        if (_completeShown) return;
        _completeShown = true;

        if (_completePanel != null)
            _completePanel.SetActive(true);

        if (_completeText != null)
            _completeText.text = "关卡完成！\n<size=24>两人成功通关</size>";

        GameHUD.Instance?.ShowPuzzleComplete(LevelName);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx("PuzzleComplete");

        if (IsServer)
            StartCoroutine(ReturnToNpcRoutine());
    }

    [ClientRpc]
    void UpdateCountdownClientRpc(int seconds)
    {
        if (seconds > 0)
            GameHUD.Instance?.ShowToast($"{seconds} 秒后返回 NPC 房…", 1.1f);
    }

    /// <summary>
    /// Server: count down, then return both players to the NPC room.
    /// </summary>
    IEnumerator ReturnToNpcRoutine()
    {
        for (int i = 5; i > 0; i--)
        {
            UpdateCountdownClientRpc(i);
            yield return new WaitForSeconds(1f);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.LoadLevel("大厅");
    }
}
