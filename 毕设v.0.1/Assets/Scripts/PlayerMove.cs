using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Enhanced player controller: CharacterController movement, gravity, animation,
/// footstep audio, portal velocity, and HD2D camera follow.
/// Preserves all existing Netcode networking logic.
/// </summary>
public class PlayerMove : NetworkBehaviour
{
    NetworkVariable<Vector3> SyncPos = new NetworkVariable<Vector3>();
    NetworkVariable<Quaternion> SyncRot = new NetworkVariable<Quaternion>();

    // Player number: 0 = P1 (Host), 1 = P2 (Client)
    public NetworkVariable<int> PlayerNumber = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Animation sync: 0=idle, 1=down, 2=up, 3=left, 4=right
    NetworkVariable<int> SyncFacing = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Spawn Positions")]
    [SerializeField] Vector3 _p1Spawn = new Vector3(-3f, 1f, 0f);
    [SerializeField] Vector3 _p2Spawn = new Vector3(3f, 1f, 0f);

    [Header("Movement")]
    [SerializeField] float speed = 5f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Gravity")]
    [SerializeField] float gravity = -25f;
    [SerializeField] float groundSnapForce = -2f;
    [SerializeField] float maxVelocityY = -40f;
    float _verticalVelocity;
    Vector3 _portalVelocity;
    bool _hasPortalVelocity;

    [Header("Footsteps")]
    [SerializeField] float footstepInterval = 0.35f;
    [SerializeField] float footstepPitchVariation = 0.1f;
    float _footstepTimer;

    [Header("Camera")]
    [SerializeField] Vector3 cameraOffset = new Vector3(0, 4.5f, -6f);
    [SerializeField] float cameraFollowSpeed = 5f;
    [Tooltip("注视点相对玩家的高度：>0 时玩家靠画面下方，能看更多前方")]
    [SerializeField] float cameraLookAtHeight = 1.2f;
    [Tooltip("与目标距离超过该值时镜头直接就位（出生/传送，不甩尾）")]
    [SerializeField] float cameraSnapDistance = 6f;

    [Header("Camera Collision")]
    [Tooltip("从玩家向镜头目标做球检测，命中墙时把镜头收到墙前")]
    [SerializeField] float cameraCollisionRadius = 0.25f;
    [SerializeField] LayerMask cameraCollisionMask = ~0;
    [Tooltip("镜头至少高于玩家的高度")]
    [SerializeField] float cameraMinHeight = 2.8f;
    [Tooltip("镜头至少保持在玩家北侧这么远（锁朝向，防止操作反转）")]
    [SerializeField] float cameraMinAheadZ = 0.45f;
    [Tooltip("视线 Z 分量下限：镜头永不掉头，操作方向全场景一致")]
    [SerializeField] float cameraYawLockZ = 0.35f;

    int _cameraIgnoreMask = -2; // -2 = not resolved yet

    CharacterController _cc;
    Animator _animator;
    Camera _mainCamera;
    Vector2 _movementInput;
    bool _isMoving;
    bool _spawned;
    int _lastFacing;

    [Header("Position Sync")]
    [SerializeField] float _syncInterval = 0.1f;
    float _syncTimer;
    Vector3 _lastSentPos;

    [Header("Pushing")]
    [Tooltip("推挤可搬运箱（PickupCube）时的速度系数：1 = 与玩家同速。\nCharacterController 本身不会推动刚体，必须在这里显式施加。")]
    [SerializeField] float pushSpeedFactor = 1f;

    [Header("P2 Appearance")]
    [SerializeField] RuntimeAnimatorController _p2AnimatorController;
    [SerializeField] Sprite _p2IdleSprite;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _mainCamera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        int pNum = (int)OwnerClientId;

        // Only server writes NetworkVariables
        if (IsServer)
        {
            PlayerNumber.Value = pNum;

            // Initial spawn position (per-level spawns are handled by LevelSpawnManager)
            Vector3 spawnPos = pNum == 0 ? _p1Spawn : _p2Spawn;

            _cc.enabled = false;
            transform.position = spawnPos;
            _cc.enabled = true;

            SyncPos.Value = spawnPos;
            SyncRot.Value = Quaternion.identity;
        }

        _spawned = true;

        // Subscribe to animation changes for non-owners
        if (!IsOwner)
            SyncFacing.OnValueChanged += OnFacingChanged;

        // Update HUD label
        if (IsOwner && GameHUD.Instance != null)
            GameHUD.Instance.SetPlayerLabel(pNum == 0 ? "P1 (Host)" : "P2 (Client)");

        // Host (P1) keeps prefab default appearance; joining player (P2) swaps.
        // Runs on all machines so everyone sees the correct models.
        ApplyPlayerAppearance(pNum);
    }

    /// <summary>
    /// Player 1 (host, clientId 0) uses the prefab's default player1 sprite set.
    /// Player 2 (joining client, clientId 1) swaps to the player2 sprite set.
    /// </summary>
    public void ApplyPlayerAppearance(int playerNum)
    {
        if (playerNum == 0) return;

        if (_animator != null && _p2AnimatorController != null)
            _animator.runtimeAnimatorController = _p2AnimatorController;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && _p2IdleSprite != null)
            sr.sprite = _p2IdleSprite;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            SyncFacing.OnValueChanged -= OnFacingChanged;
    }

    void OnFacingChanged(int oldVal, int newVal)
    {
        if (_animator != null)
            _animator.SetInteger("facing", newVal);
    }

    void Update()
    {
        if (!IsOwner || !_spawned) return;
        if (ChatSystem.IsChatOpen) return;

        Move();
        SetAnimation();
        HandleFootsteps();
        FollowCamera();
        UpdateTransform();
    }

    void FixedUpdate()
    {
        if (IsOwner || !_spawned) return;
        SyncTransform();
    }

    /// <summary>
    /// Pushing a carryable crate. A CharacterController never moves a Rigidbody by
    /// itself, so the push is applied explicitly: the host applies it directly, a
    /// client relays it to the server (server-authoritative) — otherwise only the
    /// host player would be able to push crates at all.
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!_spawned) return;
        if (hit.rigidbody == null || hit.rigidbody.isKinematic) return;

        var cube = hit.rigidbody.GetComponent<PickupCube>();
        if (cube == null) return; // only carryable crates

        Vector3 v = ComputePushVelocity(hit);
        if (v.sqrMagnitude < 0.01f) return;

        if (IsServer)
            cube.ApplyPush(v);
        else if (IsOwner)
            RelayPush(cube, v);
    }

    Vector3 ComputePushVelocity(ControllerColliderHit hit)
    {
        Vector3 v = _cc != null ? _cc.velocity : Vector3.zero;
        v.y = 0f;
        if (v.sqrMagnitude >= 0.01f) return v * pushSpeedFactor;

        v = hit.moveDirection;
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f) return Vector3.zero;
        return v.normalized * speed * pushSpeedFactor;
    }

    const float PUSH_RELAY_INTERVAL = 0.08f;
    float _pushRelayTimer;

    void RelayPush(PickupCube cube, Vector3 velocity)
    {
        _pushRelayTimer -= Time.deltaTime;
        if (_pushRelayTimer > 0f) return;
        _pushRelayTimer = PUSH_RELAY_INTERVAL;
        PushCubeServerRpc(cube.NetworkObject, velocity);
    }

    /// <summary>
    /// Server: apply a client's push request. Soft-validated — the sender really
    /// has to be standing next to a carryable crate, with a sane speed.
    /// </summary>
    [ServerRpc]
    void PushCubeServerRpc(NetworkObjectReference cubeRef, Vector3 velocity, ServerRpcParams rpcParams = default)
    {
        if (!_spawned) return;

        var senderId = rpcParams.Receive.SenderClientId;
        var playerObj = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(senderId)
            : null;
        if (playerObj == null) return;

        if (!cubeRef.TryGet(out var cubeNo)) return;
        var cube = cubeNo.GetComponent<PickupCube>();
        if (cube == null) return;

        if (Vector3.Distance(playerObj.transform.position, cubeNo.transform.position) > 1.6f) return;

        Vector3 v = new Vector3(velocity.x, 0f, velocity.z);
        float sq = v.sqrMagnitude;
        if (sq < 0.01f || sq > 100f) return; // ignore noise / absurd speeds

        cube.ApplyPush(v);
    }

    void SyncTransform()
    {
        transform.position = SyncPos.Value;
        transform.rotation = SyncRot.Value;

        // Apply animation on non-owner clients
        if (_animator != null)
            _animator.SetInteger("facing", SyncFacing.Value);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        _movementInput = new Vector2(h, v);
        _isMoving = _movementInput.magnitude > 0.1f;

        // Gravity
        bool isGrounded = _cc.isGrounded;
        if (isGrounded && _verticalVelocity < 0)
            _verticalVelocity = groundSnapForce;
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
            if (_verticalVelocity < maxVelocityY)
                _verticalVelocity = maxVelocityY;
        }

        // Calculate velocity
        Vector3 velocity = new Vector3(_movementInput.x, 0, _movementInput.y) * speed;
        velocity.y = _verticalVelocity;

        // Apply portal velocity if any
        if (_hasPortalVelocity)
        {
            velocity.x = _portalVelocity.x;
            velocity.z = _portalVelocity.z;
            if (_portalVelocity.y > 0)
                _verticalVelocity = _portalVelocity.y;
            _hasPortalVelocity = false;
        }

        _cc.Move(velocity * Time.deltaTime);
    }

    void SetAnimation()
    {
        if (_animator == null) return;

        int facing = 0; // 0=idle, 1=down, 2=up, 3=left, 4=right
        if (_isMoving)
        {
            if (Mathf.Abs(_movementInput.y) >= Mathf.Abs(_movementInput.x))
                facing = _movementInput.y > 0 ? 2 : 1;
            else
                facing = _movementInput.x > 0 ? 4 : 3;
        }

        // Apply locally
        _animator.SetInteger("facing", facing);

        // Sync to server if changed
        if (facing != _lastFacing)
        {
            _lastFacing = facing;
            if (IsServer)
                SyncFacing.Value = facing;
            else
                SetFacingServerRpc(facing);
        }
    }

    void HandleFootsteps()
    {
        if (!_isMoving || !_cc.isGrounded) { _footstepTimer = 0; return; }

        _footstepTimer += Time.deltaTime;
        if (_footstepTimer >= footstepInterval)
        {
            _footstepTimer = 0;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxAt(transform.position, "Footstep", footstepPitchVariation);
        }
    }

    void FollowCamera()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        Vector3 desired = transform.position + cameraOffset;

        // Per-scene coarse Z bounds (optional safety net)
        if (CameraBounds.Active != null)
            desired.z = CameraBounds.Active.ClampZ(desired.z);

        // Camera collision: spherecast from above the player toward the desired
        // position; if a wall sits in between, pull the camera in front of it.
        if (_cameraIgnoreMask == -2)
        {
            int ignore = LayerMask.NameToLayer("CameraIgnore");
            _cameraIgnoreMask = ignore >= 0 ? ~(1 << ignore) : ~0;
        }
        Vector3 origin = transform.position + Vector3.up * 2.3f; // above the player capsule
        Vector3 delta = desired - origin;
        float len = delta.magnitude;
        if (len > 0.01f)
        {
            Vector3 dir = delta / len;
            int mask = cameraCollisionMask.value & _cameraIgnoreMask;
            if (Physics.SphereCast(origin, cameraCollisionRadius, dir, out RaycastHit hit,
                    len, mask, QueryTriggerInteraction.Ignore))
            {
                float d = Mathf.Max(0.8f, hit.distance - cameraCollisionRadius);
                desired = origin + dir * d;
            }
        }

        // Fixed-yaw framing: the camera never slips south of the player and never
        // drops below a sane height. This is what keeps W/A/S/D consistent everywhere
        // (previously the camera flipped 180° near north walls and controls reversed).
        desired.z = Mathf.Min(desired.z, transform.position.z - cameraMinAheadZ);
        desired.y = Mathf.Max(desired.y, transform.position.y + cameraMinHeight);

        Vector3 curPos = _mainCamera.transform.position;
        // Look target, locked to the forward hemisphere (+Z) so the yaw can never flip
        Vector3 lookDir = transform.position + Vector3.up * cameraLookAtHeight - desired;
        if (lookDir.z < cameraYawLockZ) lookDir.z = cameraYawLockZ;
        Quaternion targetRot = lookDir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(lookDir)
            : _mainCamera.transform.rotation;

        if ((curPos - desired).sqrMagnitude > cameraSnapDistance * cameraSnapDistance)
        {
            // Spawn / long teleport: snap in place instead of sweeping across the level
            _mainCamera.transform.SetPositionAndRotation(desired, targetRot);
        }
        else
        {
            // Frame-rate independent exponential damping
            float k = 1f - Mathf.Exp(-cameraFollowSpeed * Time.deltaTime);
            _mainCamera.transform.position = Vector3.Lerp(curPos, desired, k);
            _mainCamera.transform.rotation = Quaternion.Slerp(
                _mainCamera.transform.rotation, targetRot, k);
        }
    }

    void UpdateTransform()
    {
        // Throttle: send at most every _syncInterval, or immediately when moved enough
        _syncTimer += Time.deltaTime;
        bool movedEnough = (transform.position - _lastSentPos).sqrMagnitude > 0.01f;
        if (_syncTimer < _syncInterval && !movedEnough) return;
        _syncTimer = 0f;
        _lastSentPos = transform.position;

        if (IsServer)
        {
            SyncPos.Value = transform.position;
            SyncRot.Value = transform.rotation;
        }
        else
        {
            UpdateTransformServerRpc(transform.position, transform.rotation);
        }
    }

    [ServerRpc]
    void UpdateTransformServerRpc(Vector3 pos, Quaternion rot)
    {
        SyncPos.Value = pos;
        SyncRot.Value = rot;
    }

    [ServerRpc]
    void SetFacingServerRpc(int facing)
    {
        SyncFacing.Value = facing;
    }

    /// <summary>
    /// Server-authoritative teleport. The owner drives its own position locally (it never
    /// reads SyncPos), so a targeted ClientRpc applies the move on the owner as well.
    /// Non-owners follow via SyncPos.
    /// </summary>
    public void ServerTeleport(Vector3 pos)
    {
        if (!IsServer) return;

        _verticalVelocity = 0f;
        if (_cc != null) _cc.enabled = false;
        transform.position = pos;
        if (_cc != null) _cc.enabled = true;

        SyncPos.Value = pos;
        SyncRot.Value = transform.rotation;

        var rpc = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
        ApplyTeleportClientRpc(pos, rpc);
    }

    [ClientRpc]
    void ApplyTeleportClientRpc(Vector3 pos, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        _verticalVelocity = 0f;
        if (_cc != null) _cc.enabled = false;
        transform.position = pos;
        if (_cc != null) _cc.enabled = true;
    }

    // Portal support
    public Vector3 GetVelocity()
    {
        Vector3 vel = new Vector3(_movementInput.x, 0, _movementInput.y) * speed;
        vel.y = _verticalVelocity;
        return vel;
    }

    public void ApplyPortalVelocity(Vector3 velocity)
    {
        _portalVelocity = velocity;
        _hasPortalVelocity = true;
    }
}
