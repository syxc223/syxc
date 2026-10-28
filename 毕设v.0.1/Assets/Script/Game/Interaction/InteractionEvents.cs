using MFramework.Event;

// 1个引用
public readonly struct InteractionChangedEvent : IEvent
{
    // 1个引用
    public readonly InteractionBase target;
    // 1个引用
    public readonly bool inRange;

    // 0个引用
    public InteractionChangedEvent(InteractionBase target, bool inRange)
    {
        this.target = target;
        this.inRange = inRange;
    }
}
public readonly struct InteractionMenuRequestEvent : IEvent
{
    public readonly InteractionBase target;

    public InteractionMenuRequestEvent(InteractionBase target)
    {
        this.target = target;
    }
}