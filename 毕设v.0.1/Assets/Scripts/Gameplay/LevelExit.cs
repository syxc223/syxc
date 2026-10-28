using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Level exit zone: both players must be in their respective exits to complete.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelExit : NetworkBehaviour
{
    [Header("Player Assignment")]
    [SerializeField] int _playerNumber = 0; // 0 = P1 zone, 1 = P2 zone

    [Header("Visuals")]
    [SerializeField] GameObject _activeVisual;

    bool _playerInZone;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        var player = other.GetComponent<PlayerMove>();
        if (player == null) return;

        int playerNum = (int)player.OwnerClientId;
        if (playerNum != _playerNumber) return;

        _playerInZone = true;
        LevelManager.Instance?.OnPlayerInExit(_playerNumber, true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        var player = other.GetComponent<PlayerMove>();
        if (player == null) return;

        int playerNum = (int)player.OwnerClientId;
        if (playerNum != _playerNumber) return;

        _playerInZone = false;
        LevelManager.Instance?.OnPlayerInExit(_playerNumber, false);
    }

    public bool IsPlayerInZone => _playerInZone;
    public int PlayerNumber => _playerNumber;
}
