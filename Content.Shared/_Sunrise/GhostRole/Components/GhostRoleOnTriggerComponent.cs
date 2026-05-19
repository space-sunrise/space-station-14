using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.GhostRole.Components;

[RegisterComponent]
public sealed partial class GhostRoleOnTriggerComponent : Component
{
    [DataField]
    public string RoleName = "sunrise-ghost-role-justice-helmet-name";

    [DataField]
    public string RoleDescription = "sunrise-ghost-role-justice-helmet-description";

    [DataField]
    public string RoleRules = "ghost-role-information-freeagent-rules";

    [DataField]
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleFreeAgent" };
}

[RegisterComponent]
public sealed partial class GhostRoleOnEquipTriggerComponent : Component;
