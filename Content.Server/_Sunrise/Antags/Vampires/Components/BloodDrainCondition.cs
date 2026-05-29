using Content.Server._Sunrise.Antags.Vampires.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(VampireSystem))]
public sealed partial class BloodDrainConditionComponent : Component
{
    [DataField]
    public float BloodDranked = 0f;
}
