using UnityEngine;

[CreateAssetMenu(fileName = "InvestigationCase", menuName = "Game/Investigation/Investigation Case")]
public class InvestigationCaseSO : ScriptableObject
{
    public string CaseId;
    public string DisplayName;

    [TextArea(2, 4)]
    public string Summary;

    public InvestigationPhase StartPhase = InvestigationPhase.FreeInvestigation;
}
