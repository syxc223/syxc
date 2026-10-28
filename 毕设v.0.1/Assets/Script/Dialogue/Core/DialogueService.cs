using System.Collections.Generic;
using UnityEngine;

public class DialogueService : Singleton<DialogueService>
{
    private DialogueSequenceSO _activeSequence;
    private int _currentNodeIndex = -1;

    private INetworkStateSync _stateSync;

    public DialogueSequenceSO ActiveSequence => _activeSequence;
    public int CurrentNodeIndex => _currentNodeIndex;
    public bool IsPlaying => _activeSequence != null && _currentNodeIndex >= 0;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void SetStateSync(INetworkStateSync stateSync)
    {
        _stateSync = stateSync;
    }

    public bool StartDialogue(DialogueSequenceSO sequence)
    {
        if (sequence == null || sequence.Nodes == null || sequence.Nodes.Length == 0)
            return false;

        _activeSequence = sequence;
        _currentNodeIndex = FindFirstVisibleNodeIndex(sequence, 0);
        return _currentNodeIndex >= 0;
    }

    public bool TryGetCurrentNode(out DialogueNode node)
    {
        node = default;
        if (!IsPlaying)
            return false;

        node = _activeSequence.Nodes[_currentNodeIndex];
        return true;
    }

    public List<DialogueChoice> GetVisibleChoices(DialogueNode node)
    {
        var result = new List<DialogueChoice>();
        if (node.Choices == null || node.Choices.Length == 0)
            return result;

        var puzzleService = PuzzleProgressService.Instance;
        for (int i = 0; i < node.Choices.Length; i++)
        {
            var choice = node.Choices[i];
            if (puzzleService != null && !puzzleService.EvaluateConditions(choice.ShowConditions))
                continue;

            result.Add(choice);
        }

        return result;
    }

    public void AdvanceAuto()
    {
        if (!TryGetCurrentNode(out var node))
            return;

        ApplyNodeRewards(node);
        MoveToNode(node.NextNodeIndex);
    }

    public void SelectChoice(DialogueChoice choice)
    {
        if (TryGetCurrentNode(out var node))
            ApplyNodeRewards(node);

        PuzzleProgressService.Instance?.TrySetFlags(choice.SetFlags);
        GrantClueRewards(choice.RewardClueIds);
        MoveToNode(choice.NextNodeIndex);
    }

    public void EndDialogue()
    {
        _activeSequence = null;
        _currentNodeIndex = -1;
    }

    private void MoveToNode(int nodeIndex)
    {
        if (_activeSequence == null)
            return;

        if (nodeIndex < 0 || nodeIndex >= _activeSequence.Nodes.Length)
        {
            EndDialogue();
            return;
        }

        _currentNodeIndex = FindFirstVisibleNodeIndex(_activeSequence, nodeIndex);
        if (_currentNodeIndex < 0)
            EndDialogue();
    }

    private int FindFirstVisibleNodeIndex(DialogueSequenceSO sequence, int startIndex)
    {
        var puzzleService = PuzzleProgressService.Instance;
        for (int i = startIndex; i < sequence.Nodes.Length; i++)
        {
            var node = sequence.Nodes[i];
            if (puzzleService == null || puzzleService.EvaluateConditions(node.ShowConditions))
                return i;
        }

        return -1;
    }

    private void ApplyNodeRewards(DialogueNode node)
    {
        if (node.SetFlags != null && node.SetFlags.Length > 0)
            PuzzleProgressService.Instance?.TrySetFlags(node.SetFlags);

        GrantClueRewards(node.RewardClueIds);
    }

    private void GrantClueRewards(string[] clueIds)
    {
        if (clueIds == null || clueIds.Length == 0)
            return;

        var puzzleService = PuzzleProgressService.Instance;
        if (puzzleService == null)
            return;

        for (int i = 0; i < clueIds.Length; i++)
        {
            var clueId = clueIds[i];
            if (string.IsNullOrEmpty(clueId))
                continue;

            puzzleService.TryCollectClueById(clueId);
        }
    }
}
