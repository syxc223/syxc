using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PuzzleSaveData
{
    public List<string> SolvedPuzzles = new();
    public List<string> CollectedClues = new();
    public List<string> Flags = new();
    public List<string> ExaminedPoints = new();
    public string ActiveCaseId = string.Empty;
    public int CasePhase;
}

public class PuzzleProgressService : Singleton<PuzzleProgressService>
{
    private const string SaveKey = "PuzzleProgress_v2";

    private readonly HashSet<string> _solvedPuzzles = new();
    private readonly HashSet<string> _collectedClues = new();
    private readonly HashSet<string> _flags = new();
    private readonly HashSet<string> _examinedPoints = new();

    private string _activeCaseId = string.Empty;
    private InvestigationPhase _casePhase = InvestigationPhase.FreeInvestigation;

    private INetworkStateSync _stateSync;

    public InvestigationPhase CasePhase => _casePhase;
    public string ActiveCaseId => _activeCaseId;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        LoadLocal();
    }

    public void SetStateSync(INetworkStateSync stateSync)
    {
        _stateSync = stateSync;
    }

    public bool HasEvidence(string evidenceId) => HasClue(evidenceId);
    public bool HasClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId))
            return false;

        return _collectedClues.Contains(clueId);
    }

    public IEnumerable<string> GetCollectedEvidenceIds() => _collectedClues;

    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
            return false;

        return _flags.Contains(flagId);
    }

    public bool HasExamined(string examinePointId)
    {
        if (string.IsNullOrEmpty(examinePointId))
            return false;

        return _examinedPoints.Contains(examinePointId);
    }

    public bool IsPuzzleSolved(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId))
            return false;

        return _solvedPuzzles.Contains(puzzleId);
    }

    public bool EvaluateConditions(PuzzleCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        for (int i = 0; i < conditions.Length; i++)
        {
            if (!EvaluateCondition(conditions[i]))
                return false;
        }

        return true;
    }

    public bool EvaluateCondition(PuzzleCondition condition)
    {
        switch (condition.Type)
        {
            case PuzzleConditionType.HasClue:
                return HasClue(condition.Id);
            case PuzzleConditionType.HasFlag:
                return HasFlag(condition.Id);
            case PuzzleConditionType.PuzzleSolved:
                return IsPuzzleSolved(condition.Id);
            case PuzzleConditionType.PuzzleNotSolved:
                return !IsPuzzleSolved(condition.Id);
            case PuzzleConditionType.HasExamined:
                return HasExamined(condition.Id);
            case PuzzleConditionType.CasePhaseAtLeast:
                if (!int.TryParse(condition.Id, out int requiredPhase))
                    return false;
                return (int)_casePhase >= requiredPhase;
            default:
                return false;
        }
    }

    public bool StartCase(string caseId, InvestigationPhase startPhase)
    {
        if (string.IsNullOrEmpty(caseId))
            return false;

        _activeCaseId = caseId;
        _casePhase = startPhase;
        SaveLocal();
        EventBus.Publish(new InvestigationPhaseChangedEvent(_casePhase));
        return true;
    }

    public bool TryAdvanceCasePhase(InvestigationPhase nextPhase)
    {
        if ((int)nextPhase <= (int)_casePhase)
            return false;

        _casePhase = nextPhase;
        SaveLocal();
        EventBus.Publish(new InvestigationPhaseChangedEvent(_casePhase));
        EventBus.Publish(new PuzzleProgressChangedEvent(_activeCaseId, PuzzleProgressChangeType.FlagSet));
        return true;
    }

    public bool TryMarkExamined(string examinePointId)
    {
        if (string.IsNullOrEmpty(examinePointId) || _examinedPoints.Contains(examinePointId))
            return false;

        _examinedPoints.Add(examinePointId);
        SaveLocal();
        EventBus.Publish(new PuzzleProgressChangedEvent(examinePointId, PuzzleProgressChangeType.FlagSet));
        return true;
    }

    public bool TryCollectEvidence(EvidenceDefinitionSO evidence)
    {
        if (evidence == null)
            return false;

        InvestigationService.Instance?.RegisterEvidence(evidence);
        if (!TryCollectClue(evidence))
            return false;

        EventBus.Publish(new EvidenceCollectedEvent(evidence));
        return true;
    }

    public bool TryCollectClue(ClueDefinitionSO clueDefinition)
    {
        if (clueDefinition == null || string.IsNullOrEmpty(clueDefinition.ClueId))
            return false;

        if (_collectedClues.Contains(clueDefinition.ClueId))
            return false;

        _collectedClues.Add(clueDefinition.ClueId);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(clueDefinition, 1);

        SaveLocal();
        _stateSync?.SyncClueState(clueDefinition.ClueId, true);
        EventBus.Publish(new ClueCollectedEvent(clueDefinition.ClueId));
        EventBus.Publish(new PuzzleProgressChangedEvent(clueDefinition.ClueId, PuzzleProgressChangeType.ClueCollected));
        return true;
    }

    public bool TryCollectClueById(string clueId)
    {
        if (string.IsNullOrEmpty(clueId) || _collectedClues.Contains(clueId))
            return false;

        _collectedClues.Add(clueId);
        SaveLocal();
        _stateSync?.SyncClueState(clueId, true);
        EventBus.Publish(new ClueCollectedEvent(clueId));
        EventBus.Publish(new PuzzleProgressChangedEvent(clueId, PuzzleProgressChangeType.ClueCollected));
        return true;
    }

    public bool TrySetFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId) || _flags.Contains(flagId))
            return false;

        _flags.Add(flagId);
        SaveLocal();
        _stateSync?.SyncDialogueFlag(flagId, true);
        EventBus.Publish(new PuzzleProgressChangedEvent(flagId, PuzzleProgressChangeType.FlagSet));
        return true;
    }

    public bool TrySetFlags(IEnumerable<string> flagIds)
    {
        bool changed = false;
        if (flagIds == null)
            return false;

        foreach (var flagId in flagIds)
        {
            if (TrySetFlag(flagId))
                changed = true;
        }

        return changed;
    }

    public bool TrySolve(PuzzleDefinitionSO puzzleDefinition)
    {
        if (puzzleDefinition == null || string.IsNullOrEmpty(puzzleDefinition.PuzzleId))
            return false;

        if (_solvedPuzzles.Contains(puzzleDefinition.PuzzleId))
            return true;

        if (!EvaluateConditions(puzzleDefinition.UnlockConditions))
            return false;

        if (!EvaluateConditions(puzzleDefinition.SolveConditions))
            return false;

        _solvedPuzzles.Add(puzzleDefinition.PuzzleId);

        if (puzzleDefinition.RewardClueIds != null)
        {
            for (int i = 0; i < puzzleDefinition.RewardClueIds.Length; i++)
            {
                var clueId = puzzleDefinition.RewardClueIds[i];
                if (string.IsNullOrEmpty(clueId) || _collectedClues.Contains(clueId))
                    continue;

                _collectedClues.Add(clueId);
                _stateSync?.SyncClueState(clueId, true);
                EventBus.Publish(new ClueCollectedEvent(clueId));
            }
        }

        SaveLocal();
        _stateSync?.SyncPuzzleState(puzzleDefinition.PuzzleId, true);
        EventBus.Publish(new PuzzleSolvedEvent(puzzleDefinition.PuzzleId));
        EventBus.Publish(new PuzzleProgressChangedEvent(puzzleDefinition.PuzzleId, PuzzleProgressChangeType.PuzzleSolved));
        return true;
    }

    public bool TrySolveByConditions(PuzzleDefinitionSO puzzleDefinition)
    {
        if (puzzleDefinition == null)
            return false;

        if (!EvaluateConditions(puzzleDefinition.SolveConditions))
            return false;

        return TrySolve(puzzleDefinition);
    }

    public void SaveLocal()
    {
        var data = new PuzzleSaveData
        {
            SolvedPuzzles = new List<string>(_solvedPuzzles),
            CollectedClues = new List<string>(_collectedClues),
            Flags = new List<string>(_flags),
            ExaminedPoints = new List<string>(_examinedPoints),
            ActiveCaseId = _activeCaseId,
            CasePhase = (int)_casePhase
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void LoadLocal()
    {
        _solvedPuzzles.Clear();
        _collectedClues.Clear();
        _flags.Clear();
        _examinedPoints.Clear();
        _activeCaseId = string.Empty;
        _casePhase = InvestigationPhase.FreeInvestigation;

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            TryLoadLegacySave();
            return;
        }

        var json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
            return;

        ApplySaveData(JsonUtility.FromJson<PuzzleSaveData>(json));
    }

    private void TryLoadLegacySave()
    {
        const string legacyKey = "PuzzleProgress_v1";
        if (!PlayerPrefs.HasKey(legacyKey))
            return;

        var json = PlayerPrefs.GetString(legacyKey);
        if (string.IsNullOrEmpty(json))
            return;

        ApplySaveData(JsonUtility.FromJson<PuzzleSaveData>(json));
    }

    private void ApplySaveData(PuzzleSaveData data)
    {
        if (data == null)
            return;

        if (data.SolvedPuzzles != null)
        {
            for (int i = 0; i < data.SolvedPuzzles.Count; i++)
                _solvedPuzzles.Add(data.SolvedPuzzles[i]);
        }

        if (data.CollectedClues != null)
        {
            for (int i = 0; i < data.CollectedClues.Count; i++)
                _collectedClues.Add(data.CollectedClues[i]);
        }

        if (data.Flags != null)
        {
            for (int i = 0; i < data.Flags.Count; i++)
                _flags.Add(data.Flags[i]);
        }

        if (data.ExaminedPoints != null)
        {
            for (int i = 0; i < data.ExaminedPoints.Count; i++)
                _examinedPoints.Add(data.ExaminedPoints[i]);
        }

        _activeCaseId = data.ActiveCaseId ?? string.Empty;
        _casePhase = (InvestigationPhase)Mathf.Clamp(data.CasePhase, 0, (int)InvestigationPhase.Resolved);

        EventBus.Publish(new PuzzleProgressChangedEvent(string.Empty, PuzzleProgressChangeType.SaveLoaded));
    }

    public void ResetAllProgress()
    {
        _solvedPuzzles.Clear();
        _collectedClues.Clear();
        _flags.Clear();
        _examinedPoints.Clear();
        _activeCaseId = string.Empty;
        _casePhase = InvestigationPhase.FreeInvestigation;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey("PuzzleProgress_v1");
        PlayerPrefs.Save();
        EventBus.Publish(new PuzzleProgressChangedEvent(string.Empty, PuzzleProgressChangeType.SaveLoaded));
    }
}
