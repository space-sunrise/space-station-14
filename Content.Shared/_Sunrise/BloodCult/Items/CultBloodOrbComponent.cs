using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class CultBloodOrbComponent : Component
{
    [DataField("bloodCharges")]
    public FixedPoint2 BloodCharges;

    [DataField("breakSound")]
    public SoundSpecifier? BreakSound = new SoundCollectionSpecifier("GlassBreak");

    [DataField("damagePerBlood")] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamagePerBlood = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() // По итогу это выходит 5 урона с орба на 50 крови
        {
            { "Slash", 0.05 },
            { "Bloodloss", 0.05 },
        },
    };
}
