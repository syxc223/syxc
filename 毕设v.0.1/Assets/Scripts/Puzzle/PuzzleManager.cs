using UnityEngine;
using Unity.Netcode;

/// <summary>Manages overall puzzle state and completion.</summary>
public class PuzzleManager : NetworkBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    readonly NetworkVariable<bool> _puzzleComplete = new();

    [SerializeField] string puzzleName = "Chamber 01";
    [SerializeField] float completeDelay = 2f;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _puzzleComplete.OnValueChanged += OnCompleteChanged;
    }

    void OnCompleteChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            Debug.Log($"[PuzzleManager] {puzzleName} Complete!");
            GameHUD.Instance?.ShowPuzzleComplete(puzzleName);
        }
        else
        {
            GameHUD.Instance?.HidePuzzleComplete();
        }
    }

    public void OnExitZoneStateChanged(bool complete)
    {
        if (IsServer && complete && !_puzzleComplete.Value)
        {
            _puzzleComplete.Value = true;
        }
        else if (IsServer && !complete && _puzzleComplete.Value)
        {
            _puzzleComplete.Value = false;
        }
    }

    public bool IsComplete => _puzzleComplete.Value;
    public string PuzzleName => puzzleName;
}
