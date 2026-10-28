using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleDefinition", menuName = "Game/Puzzle/Puzzle Definition")]
public class PuzzleDefinitionSO : ScriptableObject
{
    public string PuzzleId;
    public string DisplayName;

    [TextArea(2, 4)]
    public string Description;

    public PuzzleCondition[] UnlockConditions;
    public PuzzleCondition[] SolveConditions;
    public string[] RewardClueIds;
}
