using Content.Shared.FixedPoint;

namespace Content.Server.Saw;


[RegisterComponent]
public sealed partial class SawComponent : Component
{
    [DataField]
    public EntityUid? EatenMind = null;

    [DataField]
    public FixedPoint2 HungerToThresholdModifier = 1.5;
}
