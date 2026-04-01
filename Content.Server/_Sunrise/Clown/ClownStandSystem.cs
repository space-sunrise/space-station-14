using Content.Server.Actions;
using Content.Server.Guardian;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.Clown;

/// <summary>
/// Sets up a clown's stand (MobClownHulk) using the guardian system,
/// so it behaves exactly like a holoparasite — container toggle, distance retract, death link.
/// </summary>
public sealed class ClownStandSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClownStandComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, ClownStandComponent component, MapInitEvent args)
    {
        // EnsureComp triggers GuardianSystem.OnHostInit which creates GuardianContainer
        // and grants ActionToggleGuardian — we immediately replace it with our custom action.
        var hostComp = EnsureComp<GuardianHostComponent>(uid);

        if (hostComp.ActionEntity != null)
        {
            _actions.RemoveAction(uid, hostComp.ActionEntity);
            QueueDel(hostComp.ActionEntity.Value);
            hostComp.ActionEntity = null;
        }

        _actions.AddAction(uid, ref hostComp.ActionEntity, component.Action);

        // Spawn the stand and link it into the guardian container (hidden until toggled).
        var coords = _transform.GetMapCoordinates(uid);
        var stand = Spawn(component.StandPrototype, coords);

        _container.Insert(stand, hostComp.GuardianContainer);
        hostComp.HostedGuardian = stand;

        if (TryComp<GuardianComponent>(stand, out var guardianComp))
            guardianComp.Host = uid;
    }
}
