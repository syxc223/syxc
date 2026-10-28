using Game;
using UnityEngine;

[RequireComponent(typeof(InteractionBase))]
public class PuzzleTrigger : ActionBase
{
    [Header("Puzzle Trigger")]
    [SerializeField] private PuzzleDefinitionSO puzzleDefinition;

    public void Configure(PuzzleDefinitionSO puzzle)
    {
        puzzleDefinition = puzzle;
    }

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        if (puzzleDefinition == null)
        {
            Debug.LogWarning("[PuzzleTrigger] 未配置 PuzzleDefinitionSO。");
            return;
        }

        var service = PuzzleProgressService.Instance;
        if (service == null)
            return;

        if (service.IsPuzzleSolved(puzzleDefinition.PuzzleId))
        {
            Debug.Log($"[PuzzleTrigger] 谜题已解决: {puzzleDefinition.DisplayName}");
            return;
        }

        if (!service.EvaluateConditions(puzzleDefinition.UnlockConditions))
        {
            Debug.Log($"[PuzzleTrigger] 谜题尚未解锁: {puzzleDefinition.DisplayName}");
            return;
        }

        if (service.TrySolveByConditions(puzzleDefinition))
            Debug.Log($"[PuzzleTrigger] 谜题完成: {puzzleDefinition.DisplayName}");
        else
            Debug.Log($"[PuzzleTrigger] 条件未满足: {puzzleDefinition.DisplayName}");

        GameModeManager.Instance?.RequestChangeMode(GameMode.Explore);
    }
}
