using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// A carryable crate. Players pick it up with E and drop it with E.
/// Server-authoritative: pickup/drop/carry-follow are all handled by the server,
/// and the position is replicated to everyone through NetworkTransform.
/// It can also be pushed by walking into it (see PlayerMove — host applies the
/// push directly, a client relays it through PlayerMove.PushCubeServerRpc), which
/// is how the crate is handed over through the delivery hatch of the twin-warehouse level.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class PickupCube : NetworkBehaviour
{
    [SerializeField] float pickupRange = 3f;
    [SerializeField] float holdDistance = 2f;
    [SerializeField] float holdHeight = 1.5f;
    [SerializeField] float moveLerpSpeed = 10f;
    [SerializeField] float dropForce = 2f;

    [Header("Pushing")]
    [Tooltip("被推/放下后施加的阻力，避免箱子一直滑走（推力由 PlayerMove.OnControllerColliderHit 施加）")]
    [SerializeField] float pushDrag = 4f;

    readonly NetworkVariable<bool> _isHeld = new();
    readonly NetworkVariable<ulong> _holderId = new();

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.drag = pushDrag;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isHeld.OnValueChanged += OnHeldChanged;
    }

    public override void OnNetworkDespawn()
    {
        _isHeld.OnValueChanged -= OnHeldChanged;
        base.OnNetworkDespawn();
    }

    void OnHeldChanged(bool oldVal, bool newVal)
    {
        _rb.isKinematic = newVal;
        _rb.useGravity = !newVal;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfxAt(transform.position, newVal ? "CubePickup" : "CubeDrop");
    }

    void Update()
    {
        if (!IsSpawned) return;

        bool heldByMe = _isHeld.Value && _holderId.Value == NetworkManager.LocalClientId;

        if (Input.GetKeyDown(KeyCode.E) && !ChatSystem.IsChatOpen && !GameHUD.IsPaused)
        {
            if (heldByMe)
                DropServerRpc();
            else if (!_isHeld.Value)
                TryPickupServerRpc();
        }

        // 服务器移动被持有的方块跟随持有者（位置经 NetworkTransform 复制）
        if (IsServer && _isHeld.Value)
        {
            var holder = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(_holderId.Value);
            if (holder != null)
            {
                Vector3 holdPos = holder.transform.position
                    + holder.transform.forward * holdDistance
                    + Vector3.up * holdHeight;
                transform.position = Vector3.Lerp(transform.position, holdPos, moveLerpSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Server only: give the crate a horizontal velocity (player push).
    /// Called by PlayerMove for both the host's own push and a relayed client push,
    /// so host and client players can push crates equally.
    /// </summary>
    public void ApplyPush(Vector3 horizontalVelocity)
    {
        if (!IsServer || !IsSpawned) return;
        if (_isHeld.Value || _rb == null || _rb.isKinematic) return;

        _rb.velocity = new Vector3(horizontalVelocity.x, _rb.velocity.y, horizontalVelocity.z);
    }

    // 方块归服务器所有，客户端没有所有权，因此 RPC 需要显式允许非 owner 调用
    [ServerRpc(RequireOwnership = false)]
    void TryPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        if (_isHeld.Value) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(senderId);
        if (playerObj == null) return;

        if (Vector3.Distance(transform.position, playerObj.transform.position) > pickupRange) return;

        _isHeld.Value = true;
        _holderId.Value = senderId;
    }

    [ServerRpc(RequireOwnership = false)]
    void DropServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (!_isHeld.Value || _holderId.Value != senderId) return;

        _isHeld.Value = false;
        _holderId.Value = ulong.MaxValue;

        var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(senderId);
        if (playerObj != null)
        {
            _rb.velocity = playerObj.transform.forward * dropForce;
        }
    }
}
