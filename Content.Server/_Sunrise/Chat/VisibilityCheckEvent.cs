using NetCord;
namespace Content.Server._Sunrise.Chat;

[ByRefEvent]
public struct EmoteVisibilityCheckEvent(EntityUid source, EntityUid? target, float range)
{
    [DataField]
    public EntityUid Source { get; } = source;

    [DataField]
    public EntityUid? Target { get; } = target;

    [DataField]
    public float Range { get; } = range;

    [DataField]
    public bool Visible { get; set; } = true;
}
