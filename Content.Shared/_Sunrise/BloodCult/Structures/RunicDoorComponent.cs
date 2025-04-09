using Content.Shared.Damage;

namespace Content.Shared._Sunrise.BloodCult.Structures;

[RegisterComponent]
public sealed partial class RunicDoorComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = null!;

    [DataField]
    public float ParalyzeTime = 3;

    [DataField]
    public float ThrowSpeed = 15F;
}
