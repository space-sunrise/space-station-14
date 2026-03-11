using Content.Shared.Damage;

namespace Content.Server._Sunrise.Dragon.Components;

[RegisterComponent]
public sealed partial class DragonDigestingComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Devourer;

    [ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier DigestionDamage = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public float DigestionDamageInterval;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextDamageAt;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DigestsAt;
}
