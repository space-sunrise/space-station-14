using Content.Shared.Damage;

namespace Content.Server._Sunrise.SharpeningSystem;

[RegisterComponent]
public sealed partial class SharpenerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public DamageSpecifier DamageBonus;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int Usages = 1;
}

[RegisterComponent]
public sealed partial class SharpenedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int AttacksLeft = 50;

    [DataField]
    public DamageSpecifier DamageBonus = new();
}
