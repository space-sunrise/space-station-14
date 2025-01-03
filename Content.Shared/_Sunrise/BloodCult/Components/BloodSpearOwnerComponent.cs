namespace Content.Shared._Sunrise.BloodCult.Components;

[RegisterComponent]
public sealed partial class BloodSpearOwnerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public new EntityUid? Spear;

    [DataField("returnSpearActionId"), ViewVariables(VVAccess.ReadWrite)]
    public string ReturnSpearActionId = "ActionCultReturnBloodSpear";

    [DataField("maxReturnDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxReturnDistance = 15;
}
