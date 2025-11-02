namespace Content.Server._Sunrise.DragonsBrood;

[RegisterComponent]
public sealed partial class DragonsBroodComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid MotherRift;
}
