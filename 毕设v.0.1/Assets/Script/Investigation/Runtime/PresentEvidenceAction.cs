using Game;
using System;
using UnityEngine;

[Serializable]
public struct EvidenceReaction
{
    public string EvidenceId;
    [TextArea(2, 4)]
    public string ReactionText;
    public DialogueSequenceSO ReactionDialogue;
    public string[] SetFlags;
}

/// <summary>
/// 向 NPC 出示证物。类似弹丸论破「言弹」或恶狼游戏中的举证对话。
/// </summary>
[RequireComponent(typeof(InteractionBase))]
public class PresentEvidenceAction : ActionBase
{
    [Header("Present Evidence")]
    [SerializeField] private string targetId;
    [TextArea(2, 4)]
    [SerializeField] private string promptText = "请选择要出示的证物。";
    [SerializeField] private EvidenceReaction[] reactions;
    [TextArea(2, 3)]
    [SerializeField] private string defaultRejectText = "这条证物似乎与现在的情况无关……";

    public string TargetId => string.IsNullOrEmpty(targetId) ? gameObject.name : targetId;
    public string PromptText => promptText;
    public string DefaultRejectText => defaultRejectText;
    public EvidenceReaction[] Reactions => reactions;

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new EvidencePresentRequestEvent(this));
    }

    public bool TryPresentEvidence(string evidenceId, out string feedback, out DialogueSequenceSO dialogue)
    {
        feedback = defaultRejectText;
        dialogue = null;

        if (string.IsNullOrEmpty(evidenceId))
            return false;

        if (reactions != null)
        {
            for (int i = 0; i < reactions.Length; i++)
            {
                if (reactions[i].EvidenceId != evidenceId)
                    continue;

                feedback = reactions[i].ReactionText;
                dialogue = reactions[i].ReactionDialogue;
                PuzzleProgressService.Instance?.TrySetFlags(reactions[i].SetFlags);
                return true;
            }
        }

        return false;
    }
}
