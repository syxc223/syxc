using Game;
using UnityEngine;

[RequireComponent(typeof(InteractionBase))]
public class InspectableObject : ActionBase
{
    [Header("Inspect")]
    [SerializeField] private string inspectTitle;
    [TextArea(2, 6)]
    [SerializeField] private string inspectDescription;
    [SerializeField] private Sprite inspectImage;

    public string InspectTitle => inspectTitle;
    public string InspectDescription => inspectDescription;
    public Sprite InspectImage => inspectImage;

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new InspectRequestEvent(this));
    }
}

public readonly struct InspectRequestEvent : MFramework.Event.IEvent
{
    public readonly InspectableObject Target;

    public InspectRequestEvent(InspectableObject target)
    {
        Target = target;
    }
}
