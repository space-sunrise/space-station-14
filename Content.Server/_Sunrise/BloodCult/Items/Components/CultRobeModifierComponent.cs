namespace Content.Server._Sunrise.BloodCult.Items.Components;

[RegisterComponent]
public sealed partial class CultRobeModifierComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("speedModifier")]
    public float SpeedModifier = 1.45f;

    [ViewVariables(VVAccess.ReadOnly), DataField("damageModifierSetId")]
    public string DamageModifierSetId = "CultRobe";

    public string? StoredDamageSetId { get; set; }
}
