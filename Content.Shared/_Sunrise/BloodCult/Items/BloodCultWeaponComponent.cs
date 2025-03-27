using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class BloodCultWeaponComponent : Component
{
    [DataField("convertedId"), ViewVariables(VVAccess.ReadWrite)]
    public string ConvertedId = "Holywater";

    [DataField("ConvertedToId"), ViewVariables(VVAccess.ReadWrite)]
    public string ConvertedToId = "Unholywater";

    [DataField("convertTileSound")]
    public SoundSpecifier ConvertHolyWaterSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/curse.ogg");

    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField("stuhTime"), ViewVariables(VVAccess.ReadWrite)]
    public int StuhTime;
}
