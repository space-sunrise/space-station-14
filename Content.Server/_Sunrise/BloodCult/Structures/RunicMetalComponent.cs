using Content.Shared._Sunrise.BloodCult.Structures;

namespace Content.Server._Sunrise.BloodCult.Structures;

[RegisterComponent]
public sealed partial class RunicMetalComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("delay")]
    public float Delay = 1;
}
