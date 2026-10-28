using UnityEngine;

[CreateAssetMenu(fileName = "ClueDefinition", menuName = "Game/Puzzle/Clue Definition")]
public class ClueDefinitionSO : ItemDefinitionSO
{
    public string ClueId;
    public string RelatedPuzzleId;

    private void OnEnable()
    {
        itemType = ItemType.Consumable;
        itemIconKey = ItemIconKey.KeyItem;
        MaxStack = 1;
    }
}
