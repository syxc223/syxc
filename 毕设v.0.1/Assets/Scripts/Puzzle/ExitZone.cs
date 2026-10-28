using UnityEngine;
using Unity.Netcode;

/// <summary>Goal zone that triggers puzzle completion when both players enter.</summary>
[RequireComponent(typeof(Collider))]
public class ExitZone : NetworkBehaviour
{
    [SerializeField] Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] Color activeColor = new Color(0.2f, 1f, 0.4f, 0.8f);

    readonly NetworkVariable<int> _playersInZone = new();

    Renderer _renderer;
    int _requiredPlayers = 2;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _renderer.material.color = inactiveColor;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _playersInZone.OnValueChanged += OnPlayersChanged;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            _playersInZone.Value++;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxAt(transform.position, "ZoneEnter");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            _playersInZone.Value = Mathf.Max(0, _playersInZone.Value - 1);
        }
    }

    void OnPlayersChanged(int oldVal, int newVal)
    {
        bool complete = newVal >= _requiredPlayers;
        if (_renderer != null)
        {
            _renderer.material.color = complete ? activeColor : inactiveColor;
            if (complete)
            {
                _renderer.material.EnableKeyword("_EMISSION");
                _renderer.material.SetColor("_EmissionColor", activeColor * 2f);
            }
        }

        if (complete && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx("PuzzleComplete");

        PuzzleManager.Instance?.OnExitZoneStateChanged(complete);
    }

    public void SetRequiredPlayers(int count) => _requiredPlayers = count;
}
