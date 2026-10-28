using Game;
using UnityEngine;

[RequireComponent(typeof(InteractionBase))]
public class CluePickup : ActionBase
{
    [Header("Clue Pickup")]
    [SerializeField] private ClueDefinitionSO clueDefinition;
    [SerializeField] private bool hideAfterCollect = true;

    public void Configure(ClueDefinitionSO clue)
    {
        clueDefinition = clue;
    }

    private InteractionBase _interactionBase;

    private void Awake()
    {
        _interactionBase = GetComponent<InteractionBase>();
    }

    public override bool CanExecute(AllyDefinitionSO interactor)
    {
        if (clueDefinition == null)
            return false;

        var service = PuzzleProgressService.Instance;
        return service != null && !service.HasClue(clueDefinition.ClueId);
    }

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        Execute();
        GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
    }

    public override void Execute()
    {
        if (clueDefinition == null || PuzzleProgressService.Instance == null)
            return;

        if (!PuzzleProgressService.Instance.TryCollectClue(clueDefinition))
            return;

        if (hideAfterCollect)
        {
            if (_interactionBase != null)
                _interactionBase.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
