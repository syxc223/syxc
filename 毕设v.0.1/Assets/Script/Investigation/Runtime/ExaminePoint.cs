using Game;
using UnityEngine;

/// <summary>
/// 场景调查点：首次调查可获取证物，之后可重复查看描述。
/// </summary>
[RequireComponent(typeof(InteractionBase))]
public class ExaminePoint : ActionBase
{
    [Header("Examine")]
    [SerializeField] private string examinePointId;
    [SerializeField] private string title;
    [TextArea(3, 6)]
    [SerializeField] private string firstExamineDescription;
    [TextArea(2, 4)]
    [SerializeField] private string repeatExamineDescription;
    [SerializeField] private Sprite previewImage;
    [SerializeField] private EvidenceDefinitionSO firstExamineEvidence;
    [SerializeField] private string[] firstExamineFlags;

    public string ExaminePointId => string.IsNullOrEmpty(examinePointId) ? gameObject.name : examinePointId;

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        var service = PuzzleProgressService.Instance;
        bool alreadyExamined = service != null && service.HasExamined(ExaminePointId);

        if (!alreadyExamined)
        {
            service?.TryMarkExamined(ExaminePointId);
            service?.TrySetFlags(firstExamineFlags);

            if (firstExamineEvidence != null)
                service?.TryCollectEvidence(firstExamineEvidence);
        }

        string description = alreadyExamined && !string.IsNullOrEmpty(repeatExamineDescription)
            ? repeatExamineDescription
            : firstExamineDescription;

        EventBus.Publish(new ExamineRequestEvent(this, title, description, previewImage));
    }
}

public readonly struct ExamineRequestEvent : MFramework.Event.IEvent
{
    public readonly ExaminePoint Source;
    public readonly string Title;
    public readonly string Description;
    public readonly Sprite PreviewImage;

    public ExamineRequestEvent(ExaminePoint source, string title, string description, Sprite previewImage)
    {
        Source = source;
        Title = title;
        Description = description;
        PreviewImage = previewImage;
    }
}
