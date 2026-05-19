using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Ghost;
using Content.Shared.GhostRole.Components;
using Content.Shared.Mind.Components;

namespace Content.Server.GhostRole;

public sealed class MakeGhostRoleOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    public bool TryMakeOnTrigger(EntityUid target, GhostRoleOnTriggerComponent trigger)
    {
        if (HasComp<GhostComponent>(target))
            return false;

        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.HasMind)
            return false;

        var ghostRole = EnsureComp<GhostRoleComponent>(target);
        EnsureComp<GhostTakeoverAvailableComponent>(target);

        ghostRole.RoleName = trigger.RoleName;
        ghostRole.RoleDescription = trigger.RoleDescription;
        ghostRole.RoleRules = trigger.RoleRules;
        ghostRole.MindRoles = trigger.MindRoles;
        return true;
    }

    public void CleanupGhostRole(EntityUid target)
    {
        if (!HasComp<GhostRoleComponent>(target) && !HasComp<GhostTakeoverAvailableComponent>(target))
            return;

        RemComp<GhostTakeoverAvailableComponent>(target);

        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.HasMind)
            _ghost.OnGhostAttempt(mindContainer.Mind!.Value, false, true, true);

        RemComp<GhostRoleComponent>(target);
    }
}
