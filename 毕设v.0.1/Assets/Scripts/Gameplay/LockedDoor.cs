using UnityEngine;
using Unity.Netcode;

/// <summary>
/// The gate of a co-op puzzle: driven by pressure plates.
///   _requireAllPlates = false → opens while ANY linked plate is pressed
///   _requireAllPlates = true  → opens only while ALL linked plates are pressed
///                               (two players — or two weights — at the same time)
/// Plates are allowed to live in the OTHER hallway: the door you need can only be
/// opened by your partner, which is the whole point of the twin-warehouse level.
/// Slides vertically and disables its collider while open. Server-authoritative.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LockedDoor : NetworkBehaviour
{
    NetworkVariable<bool> _isOpen = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Linked Plates (may be in the other hallway)")]
    [SerializeField] PressurePlate[] _plates;

    [Header("Logic")]
    [Tooltip("true = 所有压力板必须同时被压住（双板同时）；false = 任一压力板被压住即可")]
    [SerializeField] bool _requireAllPlates = false;

    [Header("Feedback")]
    [Tooltip("提示用名称，例如「左厅第 1 道闸门」")]
    [SerializeField] string _displayName = "闸门";
    [Tooltip("开合时在本机 HUD 提示，让玩家知道（可能是对面走廊的）门状态变了")]
    [SerializeField] bool _announceState = true;

    [Header("Animation")]
    [SerializeField] Vector3 _openOffset = new Vector3(0, 4f, 0);
    [SerializeField] float _animSpeed = 2f;

    Vector3 _closedPos;
    Vector3 _openPos;

    void Awake()
    {
        _closedPos = transform.localPosition;
        _openPos = _closedPos + _openOffset;
    }

    public override void OnNetworkSpawn()
    {
        _isOpen.OnValueChanged += OnDoorStateChanged;
        UpdateDoorVisual();
    }

    public override void OnNetworkDespawn()
    {
        _isOpen.OnValueChanged -= OnDoorStateChanged;
    }

    void OnDoorStateChanged(bool oldVal, bool newVal)
    {
        UpdateDoorVisual();

        if (_announceState)
        {
            string label = string.IsNullOrEmpty(_displayName) ? "闸门" : _displayName;
            GameHUD.Instance?.ShowToast($"{label} {(newVal ? "已开启" : "已关闭")}", 2.5f);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfxAt(transform.position, newVal ? "DoorOpen" : "DoorClose");
    }

    void Update()
    {
        // Server: evaluate the linked plates every frame
        if (IsServer)
        {
            bool shouldOpen = EvaluatePlates();
            if (shouldOpen != _isOpen.Value)
                _isOpen.Value = shouldOpen;
        }

        // All clients: smooth movement
        Vector3 targetPos = _isOpen.Value ? _openPos : _closedPos;
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, targetPos, _animSpeed * Time.deltaTime);
    }

    bool EvaluatePlates()
    {
        if (_plates == null || _plates.Length == 0) return false;

        if (_requireAllPlates)
        {
            for (int i = 0; i < _plates.Length; i++)
            {
                // An unassigned plate can never be satisfied — keeps AND mode safe
                if (_plates[i] == null || !_plates[i].IsPressed) return false;
            }
            return true;
        }

        for (int i = 0; i < _plates.Length; i++)
        {
            if (_plates[i] != null && _plates[i].IsPressed) return true;
        }
        return false;
    }

    void UpdateDoorVisual()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = !_isOpen.Value;
    }

    public bool IsOpen => _isOpen.Value;
}
