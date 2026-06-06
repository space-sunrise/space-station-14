using Content.Shared.Roles;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.GhostRole.Components;

/// <summary>
/// Makes the target available as a ghost role when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MakeGhostRoleOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The localization string for the ghost role name.
    /// </summary>
    [DataField]
    public string RoleName = "sunrise-ghost-role-justice-helmet-name";

    /// <summary>
    /// The localization string for the ghost role description.
    /// </summary>
    [DataField]
    public string RoleDescription = "sunrise-ghost-role-justice-helmet-description";

    /// <summary>
    /// The localization string for the ghost role rules.
    /// </summary>
    [DataField]
    public string RoleRules = "ghost-role-information-freeagent-rules";

    /// <summary>
    /// Mind roles assigned when the ghost role is taken.
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleFreeAgent" };
}

/// <summary>
/// Removes the target ghost role when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveGhostRoleOnTriggerComponent : BaseXOnTriggerComponent;
