using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the team of players is on the emergency shuttle's grid when docking to CentCom.
/// </summary>
[RegisterComponent, Access(typeof(EscapeShuttleConditionSystem))]
public sealed partial class EscapeShuttleTeamConditionComponent : Component
{
}
