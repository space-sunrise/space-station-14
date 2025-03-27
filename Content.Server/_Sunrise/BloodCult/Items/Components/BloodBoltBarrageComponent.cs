using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.BloodCult.Items.Components;

[RegisterComponent]
public sealed partial class BloodBoltBarrageComponent : Component
{
    public FixedPoint2 ShotCost = 12;
}
