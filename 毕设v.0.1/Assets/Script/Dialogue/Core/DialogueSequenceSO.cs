using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Game/Dialogue/Dialogue Sequence")]
public class DialogueSequenceSO : ScriptableObject
{
    public string DialogueId;
    public DialogueNode[] Nodes;
}
