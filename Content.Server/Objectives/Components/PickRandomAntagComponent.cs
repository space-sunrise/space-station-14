using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class PickRandomAntagComponent : Component
{
}
