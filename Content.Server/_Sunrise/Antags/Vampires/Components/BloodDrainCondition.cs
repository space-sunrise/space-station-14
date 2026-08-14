using Content.Server._Sunrise.Antags.Vampires.Systems;

namespace Content.Server._Sunrise.Antags.Vampires.Components;

[RegisterComponent, Access(typeof(VampireSystem))]
public sealed partial class BloodDrainConditionComponent : Component
{
    [DataField]
    public float BloodDrained = 0f;
}
