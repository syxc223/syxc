using UnityEngine;

/// <summary>
/// 证物定义。ClueId 作为全局唯一 EvidenceId 使用。
/// </summary>
[CreateAssetMenu(fileName = "EvidenceDefinition", menuName = "Game/Investigation/Evidence Definition")]
public class EvidenceDefinitionSO : ClueDefinitionSO
{
    public EvidenceCategory Category = EvidenceCategory.Physical;

    [TextArea(2, 5)]
    public string DetailDescription;

    public Sprite DetailImage;

    public string EvidenceId => ClueId;
}
