using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Ghost;
using Content.Shared.GhostRole.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;

namespace Content.Server.GhostRole;

public sealed class MakeGhostRoleOnTriggerSystem : XOnTriggerSystem<MakeGhostRoleOnTriggerComponent>
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    protected override void OnTrigger(Entity<MakeGhostRoleOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (TryMakeOnTrigger(target, ent.Comp))
            args.Handled = true;
    }

    public bool TryMakeOnTrigger(EntityUid target, MakeGhostRoleOnTriggerComponent trigger)
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

public sealed class RemoveGhostRoleOnTriggerSystem : XOnTriggerSystem<RemoveGhostRoleOnTriggerComponent>
{
    [Dependency] private readonly MakeGhostRoleOnTriggerSystem _makeGhostRole = default!;

    protected override void OnTrigger(Entity<RemoveGhostRoleOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _makeGhostRole.CleanupGhostRole(target);
        args.Handled = true;
    }
}
