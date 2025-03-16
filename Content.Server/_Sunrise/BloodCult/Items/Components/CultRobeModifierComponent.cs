namespace Content.Server._Sunrise.BloodCult.Items.Components;

[RegisterComponent]
public sealed partial class CultRobeModifierComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("damageModifierSetId")]
    public string DamageModifierSetId = "CultRobe";

    [ViewVariables(VVAccess.ReadWrite), DataField("speedModifier")]
    public float SpeedModifier = 1.45f;

    public string? StoredDamageSetId { get; set; }
}
