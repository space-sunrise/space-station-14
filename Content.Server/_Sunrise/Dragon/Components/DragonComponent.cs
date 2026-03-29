using Content.Shared.Damage;

namespace Content.Server.Dragon;

public sealed partial class DragonComponent
{
    [DataField(required: true)]
    public DamageSpecifier DamageOnDevour = default!;

    [DataField(required: true)]
    public DamageSpecifier DigestionDamage = default!;

    [DataField]
    public float DigestionDamageInterval = 30f;

    [DataField]
    public float DigestionDuration = 720f;
}
