using UnityEngine;
using Unity.Netcode;

/// <summary>A door that opens/closes based on puzzle state. Moves vertically.</summary>
[RequireComponent(typeof(Collider))]
public class PuzzleDoor : NetworkBehaviour
{
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float openYOffset = 3f;

    readonly NetworkVariable<bool> _isOpen = new();
    Vector3 _closedPos;
    Vector3 _openPos;

    void Awake()
    {
        _closedPos = transform.position;
        _openPos = _closedPos + Vector3.up * openYOffset;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isOpen.OnValueChanged += (_, open) =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxAt(transform.position, open ? "DoorOpen" : "DoorClose");
        };
    }

    void Update()
    {
        Vector3 target = _isOpen.Value ? _openPos : _closedPos;
        transform.position = Vector3.Lerp(transform.position, target, moveSpeed * Time.deltaTime);

        // Toggle collider based on openness
        float threshold = 0.05f;
        bool shouldBlock = Mathf.Abs(transform.position.y - _closedPos.y) < threshold;
        GetComponent<Collider>().enabled = shouldBlock && !_isOpen.Value;
    }

    /// <summary>Called by linked pressure plates or laser receivers.</summary>
    public void SetOpen(bool open)
    {
        if (IsServer)
            _isOpen.Value = open;
    }

    public bool IsOpen => _isOpen.Value;
}
