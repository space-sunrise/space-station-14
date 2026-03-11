namespace Content.Shared._Sunrise.Bed;

[RegisterComponent]
public sealed partial class BedHealModifierClothingComponent : Component
{
    [DataField]
    public float Multiplier = 0.25f;
}
