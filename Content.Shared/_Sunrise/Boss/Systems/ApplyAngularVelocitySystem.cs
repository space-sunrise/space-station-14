using Content.Shared._Sunrise.Boss.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Sunrise.Boss.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ApplyAngularVelocitySystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ApplyAngularVelocityComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ApplyAngularVelocityComponent component, ComponentInit args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;
        _physics.SetAngularVelocity(uid, component.Impulse, body: physics);
    }
}
