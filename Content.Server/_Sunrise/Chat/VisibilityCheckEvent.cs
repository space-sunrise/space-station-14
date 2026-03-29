namespace Content.Server._Sunrise.Chat;

public sealed class VisibilityCheckEvent : CancellableEntityEventArgs
{
    public EntityUid Source { get; }
    public EntityUid? Target { get; }
    public float Range { get; }

    public VisibilityCheckEvent(EntityUid source, EntityUid? target, float range)
    {
        Source = source;
        Target = target;
        Range = range;
    }
}
