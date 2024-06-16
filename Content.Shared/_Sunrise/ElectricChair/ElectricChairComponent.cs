namespace Content.Shared._Sunrise.ElectricChair;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ElectricChairComponent : Component
{
    [DataField]
    public int ShockDamage = 200;

    [DataField]
    public int ShockDelay = 30;
}
