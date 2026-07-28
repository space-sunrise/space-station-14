using Content.Shared.FixedPoint;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Effects;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveVampireRejuvenateComponent : Component
{
    public int TicksRemaining;

    public TimeSpan TickInterval = TimeSpan.FromSeconds(3.5);

    [AutoPausedField]
    public TimeSpan NextTick;

    public Dictionary<string, FixedPoint2> HealGroups = [];

    public Dictionary<string, FixedPoint2> HealTypes = [];
}
