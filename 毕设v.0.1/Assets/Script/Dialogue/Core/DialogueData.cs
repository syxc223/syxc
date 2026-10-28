using System;
using UnityEngine;

[Serializable]
public struct DialogueChoice
{
    public string Text;
    public int NextNodeIndex;
    public PuzzleCondition[] ShowConditions;
    public string[] SetFlags;
    public string[] RewardClueIds;
}

[Serializable]
public struct DialogueNode
{
    public string SpeakerName;
    public Sprite Portrait;
    [TextArea(2, 6)]
    public string Text;
    public DialogueChoice[] Choices;
    public PuzzleCondition[] ShowConditions;
    public string[] SetFlags;
    public string[] RewardClueIds;
    public int NextNodeIndex;
}
