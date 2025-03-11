using Content.Shared.FixedPoint;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class CultBloodOrbComponent : Component
{
    [DataField("bloodCharges")]
    public FixedPoint2 BloodCharges;
}
