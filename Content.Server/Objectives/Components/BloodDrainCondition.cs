using Content.Server.Vampire;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(VampireSystem))]
public sealed partial class BloodDrainConditionComponent : Component
{
}