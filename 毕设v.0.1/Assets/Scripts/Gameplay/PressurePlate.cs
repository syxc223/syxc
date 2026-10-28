using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Pressure plate: a player (or a carryable crate) standing on it activates it.
/// The plate stays pressed while at least one occupant is on it, and releases the
/// moment the last occupant steps off — that is what makes "one player holds the
/// mechanism while the other walks through" possible, and what makes a single
/// player unable to solve a two-plate gate alone.
/// Server-authoritative. Occupants are detected by an overlap query on the server
/// (trigger callbacks alone are unreliable for a teleported / stationary
/// CharacterController), which also means a disconnected player can never leave
/// the plate stuck.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PressurePlate : NetworkBehaviour
{
    [Header("Linked Door")]
    [SerializeField] LockedDoor _linkedDoor;

    [Header("Occupants")]
    [Tooltip("可搬运箱（PickupCube）也能压住压力板 —— 用箱子代替人踩板是协作解谜的核心手段")]
    [SerializeField] bool _acceptPickupCubes = true;
    [Tooltip("服务器检测占位者的间隔（秒）")]
    [SerializeField] float _verifyInterval = 0.2f;

    NetworkVariable<bool> _isPressed = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server-side occupant list, keyed by NetworkObjectId (a set, so duplicate
    // events cannot corrupt the count).
    readonly HashSet<ulong> _occupants = new HashSet<ulong>();
    readonly HashSet<ulong> _fresh = new HashSet<ulong>();
    readonly Collider[] _overlapBuffer = new Collider[16];

    float _verifyTimer;

    public override void OnNetworkSpawn()
    {
        _isPressed.OnValueChanged += OnPressedChanged;
        UpdateVisual();
    }

    public override void OnNetworkDespawn()
    {
        _isPressed.OnValueChanged -= OnPressedChanged;
        if (IsServer) _occupants.Clear();
    }

    void Update()
    {
        if (!IsServer) return;

        _verifyTimer += Time.deltaTime;
        if (_verifyTimer < _verifyInterval) return;
        _verifyTimer = 0f;

        RefreshOccupants();
    }

    /// <summary>
    /// Server: rebuild the occupant set from what physically overlaps the plate.
    /// Objects that were destroyed (player disconnect) simply disappear, so the
    /// plate can never stay pressed forever and lock the puzzle.
    /// </summary>
    void RefreshOccupants()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector3 center = b.center + Vector3.up * 0.05f;
        Vector3 half = b.extents + new Vector3(0.06f, 0.35f, 0.06f);

        int n = Physics.OverlapBoxNonAlloc(center, half, _overlapBuffer,
            Quaternion.identity, ~0, QueryTriggerInteraction.Collide);

        _fresh.Clear();
        for (int i = 0; i < n; i++)
        {
            var c = _overlapBuffer[i];
            if (c == null || c == col) continue;
            if (!TryGetOccupantId(c, out ulong id)) continue;
            _fresh.Add(id);
        }

        if (_fresh.SetEquals(_occupants)) return;

        _occupants.Clear();
        foreach (ulong id in _fresh) _occupants.Add(id);
        RefreshPressed();
    }

    /// <summary>
    /// Resolves a collider that belongs to the player or a carryable crate
    /// (colliders may live on a child of the networked root object).
    /// </summary>
    bool TryGetOccupantId(Collider other, out ulong id)
    {
        id = 0;

        bool isPlayer = other.GetComponentInParent<PlayerMove>() != null;
        bool isCube = _acceptPickupCubes && other.GetComponentInParent<PickupCube>() != null;
        if (!isPlayer && !isCube) return false;

        var no = other.GetComponentInParent<NetworkObject>();
        if (no == null) return false;

        id = no.NetworkObjectId;
        return true;
    }

    void RefreshPressed()
    {
        bool pressed = _occupants.Count > 0;
        if (_isPressed.Value != pressed)
            _isPressed.Value = pressed;
    }

    void OnPressedChanged(bool oldVal, bool newVal)
    {
        UpdateVisual();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfxAt(transform.position, newVal ? "PlatePress" : "PlateRelease");
    }

    void UpdateVisual()
    {
        // Color change: green when pressed, gray when released
        var rend = GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = _isPressed.Value
                ? new Color(0.2f, 0.8f, 0.2f)
                : new Color(0.4f, 0.4f, 0.4f);
    }

    public bool IsPressed => _isPressed.Value;
}
