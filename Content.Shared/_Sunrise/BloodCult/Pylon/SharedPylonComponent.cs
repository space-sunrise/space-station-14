using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Pylon;

[RegisterComponent]
public sealed partial class SharedPylonComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("activated")]
    public bool Activated = true;

    [DataField("airlockConvertEffect")]
    public string AirlockConvertEffect = "CultAirlockGlow";

    [ViewVariables(VVAccess.ReadOnly), DataField("airlockId")]
    public string AirlockId = "AirlockGlassCult";

    [ViewVariables(VVAccess.ReadOnly), DataField("bleedReductionAmount")]
    public float BleedReductionAmount = 1.0f;

    [ViewVariables(VVAccess.ReadOnly), DataField("bloodRefreshAmount")]
    public float BloodRefreshAmount = 1.0f;

    [ViewVariables(VVAccess.ReadOnly), DataField("burnDamageOnInteract", required: true)]
    public DamageSpecifier BurnDamageOnInteract = default!;

    [DataField("burnHandSound")]
    public SoundSpecifier BurnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [ViewVariables(VVAccess.ReadOnly), DataField("convertEverything")]
    public bool ConvertEverything;

    [DataField("convertTileSound")]
    public SoundSpecifier ConvertTileSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/curse.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("healingAuraCooldown")]
    public float HealingAuraCooldown = 5f;

    [ViewVariables(VVAccess.ReadOnly), DataField("healingAuraDamage", required: true)]
    public DamageSpecifier HealingAuraDamage = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("healingAuraRange")]
    public float HealingAuraRange = 5f;

    public TimeSpan NextHealTime = TimeSpan.Zero;

    public TimeSpan NextTileConvert = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("tileConvertCooldown")]
    public float TileConvertCooldown = 15f;

    [ViewVariables(VVAccess.ReadOnly), DataField("tileConvertEffect")]
    public string TileConvertEffect = "CultTileSpawnEffect";

    [ViewVariables(VVAccess.ReadWrite), DataField("tilesConvertRange")]
    public float TileConvertRange = 5f;

    [ViewVariables(VVAccess.ReadOnly), DataField("tileId")]
    public string TileId = "CultFloor";

    [DataField("wallConvertEffect")]
    public string WallConvertEffect = "CultWallGlow";

    [ViewVariables(VVAccess.ReadOnly), DataField("wallId")]
    public string WallId = "WallCult";
}

[Serializable, NetSerializable]
public enum PylonVisuals : byte
{
    Activated
}
