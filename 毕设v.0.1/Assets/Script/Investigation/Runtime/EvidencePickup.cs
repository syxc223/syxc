using Game;
using UnityEngine;

/// <summary>
/// 场景中可直接拾取的证物。
/// </summary>
[RequireComponent(typeof(InteractionBase))]
public class EvidencePickup : ActionBase
{
    [Header("Evidence Pickup")]
    [SerializeField] private EvidenceDefinitionSO evidence;
    [SerializeField] private bool hideAfterCollect = true;

    private InteractionBase _interactionBase;

    private void Awake()
    {
        _interactionBase = GetComponent<InteractionBase>();
    }

    public void Configure(EvidenceDefinitionSO evidenceDefinition)
    {
        evidence = evidenceDefinition;
    }

    public override bool CanExecute(AllyDefinitionSO interactor)
    {
        if (evidence == null)
            return false;

        var service = PuzzleProgressService.Instance;
        return service != null && !service.HasEvidence(evidence.EvidenceId);
    }

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        if (evidence == null || PuzzleProgressService.Instance == null)
            return;

        if (!PuzzleProgressService.Instance.TryCollectEvidence(evidence))
            return;

        if (hideAfterCollect)
        {
            if (_interactionBase != null)
                _interactionBase.enabled = false;
            gameObject.SetActive(false);
        }

        GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
    }
}
