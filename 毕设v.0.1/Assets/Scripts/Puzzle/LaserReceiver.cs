using UnityEngine;
using Unity.Netcode;

/// <summary>Receives a laser beam and activates linked puzzle elements when hit.</summary>
[RequireComponent(typeof(Collider))]
public class LaserReceiver : NetworkBehaviour
{
    [SerializeField] PuzzleDoor[] linkedDoors;
    [SerializeField] Color activatedColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] Color inactiveColor = new Color(0.3f, 0.3f, 0.3f);

    readonly NetworkVariable<bool> _isActivated = new();
    bool _currentlyHit;
    Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isActivated.OnValueChanged += OnActivatedChanged;
        UpdateVisual(_isActivated.Value);
    }

    void OnActivatedChanged(bool oldVal, bool newVal)
    {
        UpdateVisual(newVal);
        foreach (var door in linkedDoors)
        {
            if (door != null)
                door.SetOpen(newVal);
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfxAt(transform.position, newVal ? "LaserActivate" : "LaserDeactivate");
    }

    /// <summary>Called by LaserEmitter when this receiver is hit.</summary>
    public void SetHit(bool hit)
    {
        _currentlyHit = hit;
    }

    /// <summary>Called by LaserEmitter after each frame to check if still hit.</summary>
    public void CheckStillHit()
    {
        if (IsServer && _currentlyHit != _isActivated.Value)
            _isActivated.Value = _currentlyHit;
        _currentlyHit = false; // Reset for next frame
    }

    void UpdateVisual(bool activated)
    {
        if (_renderer != null)
        {
            _renderer.material.color = activated ? activatedColor : inactiveColor;
            if (activated)
            {
                _renderer.material.EnableKeyword("_EMISSION");
                _renderer.material.SetColor("_EmissionColor", activatedColor * 3f);
            }
        }
    }

    public bool IsActivated => _isActivated.Value;
}
