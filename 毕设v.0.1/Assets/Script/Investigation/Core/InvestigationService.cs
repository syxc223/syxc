using System.Collections.Generic;
using UnityEngine;

public class InvestigationService : Singleton<InvestigationService>
{
    private readonly Dictionary<string, EvidenceDefinitionSO> _evidenceRegistry = new();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterEvidence(EvidenceDefinitionSO evidence)
    {
        if (evidence == null || string.IsNullOrEmpty(evidence.EvidenceId))
            return;

        _evidenceRegistry[evidence.EvidenceId] = evidence;
    }

    public EvidenceDefinitionSO GetEvidenceDefinition(string evidenceId)
    {
        if (string.IsNullOrEmpty(evidenceId))
            return null;

        _evidenceRegistry.TryGetValue(evidenceId, out var evidence);
        return evidence;
    }

    public List<EvidenceDefinitionSO> GetCollectedEvidenceDefinitions()
    {
        var result = new List<EvidenceDefinitionSO>();
        var progress = PuzzleProgressService.Instance;
        if (progress == null)
            return result;

        foreach (var evidenceId in progress.GetCollectedEvidenceIds())
        {
            var definition = GetEvidenceDefinition(evidenceId);
            if (definition != null)
                result.Add(definition);
        }

        return result;
    }

    public bool StartCase(InvestigationCaseSO investigationCase)
    {
        if (investigationCase == null)
            return false;

        return PuzzleProgressService.Instance?.StartCase(
            investigationCase.CaseId,
            investigationCase.StartPhase) ?? false;
    }
}
