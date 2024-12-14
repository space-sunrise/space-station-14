using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class CultBloodSpearComponent : Component
{
    [DataField("stuhTime"), ViewVariables(VVAccess.ReadWrite)]
    public int StuhTime;

    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    public EntityUid? SpearOwner;

    [DataField("breakSound")]
    public SoundSpecifier? BreakSound = new SoundCollectionSpecifier("GlassBreak");
}
