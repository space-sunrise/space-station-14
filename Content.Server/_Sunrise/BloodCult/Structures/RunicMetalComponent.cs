using Content.Shared._Sunrise.BloodCult.Structures;

namespace Content.Server._Sunrise.BloodCult.Structures;

[RegisterComponent]
public sealed partial class RunicMetalComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Enum UserInterfaceKey = CultStructureCraftUiKey.Key;

    [ViewVariables(VVAccess.ReadWrite), DataField("delay")]
    public float Delay = 1;
}
