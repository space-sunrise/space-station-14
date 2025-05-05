using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.BloodCult.Items.Components;

[RegisterComponent]
public sealed partial class BloodBoltComponent : Component
{
    [DataField(required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField(required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HealConstruct = new();

    [DataField] [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 UnholyVolume = 4;

    [DataField] [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> UnholyProto = "Unholywater";
}
