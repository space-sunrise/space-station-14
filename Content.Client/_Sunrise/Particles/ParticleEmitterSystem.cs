using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Starts configured orchestras when an entity initializes and stops them when it leaves PVS.
/// </summary>
public sealed class ParticleEmitterSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly Dictionary<EntityUid, List<ActiveParticleOrchestra>> _active = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var orchestras in _active.Values)
        {
            foreach (var orchestra in orchestras)
            {
                _orchestra.Stop(orchestra);
            }
        }

        _active.Clear();
    }

    private void OnComponentInit(Entity<ParticleEmitterComponent> ent, ref ComponentInit args)
    {
        StopOrchestras(ent);

        var movement = GetInitialVelocity(ent);
        List<ActiveParticleOrchestra>? active = null;
        foreach (var specifier in ent.Comp.Orchestras)
        {
            var orchestra = _orchestra.Start(specifier, ent, movement: movement);
            if (orchestra == null)
                continue;

            active ??= new List<ActiveParticleOrchestra>(ent.Comp.Orchestras.Count);
            active.Add(orchestra);
        }

        if (active != null)
            _active[ent] = active;
    }

    private void OnComponentShutdown(Entity<ParticleEmitterComponent> ent, ref ComponentShutdown args)
    {
        StopOrchestras(ent);
    }

    private void StopOrchestras(EntityUid uid)
    {
        if (!_active.Remove(uid, out var orchestras))
            return;

        foreach (var orchestra in orchestras)
        {
            _orchestra.Stop(orchestra);
        }
    }

    private Vector2 GetInitialVelocity(EntityUid uid)
    {
        return TryComp<PhysicsComponent>(uid, out var physics)
            ? _physics.GetMapLinearVelocity(uid, physics)
            : Vector2.Zero;
    }
}
