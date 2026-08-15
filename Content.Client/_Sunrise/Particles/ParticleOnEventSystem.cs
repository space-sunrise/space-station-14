using Content.Shared._Sunrise.Particles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Handles reusable particle-orchestra bindings for thrown entities and fired projectiles.
/// </summary>
public sealed class ParticleOnEventSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, List<ActiveParticleOrchestra>> _thrownOrchestras = [];
    private readonly List<EntityUid> _staleThrownOrchestras = [];

    private EntityQuery<ParticleOnThrownComponent> _particleOnThrownQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        base.Initialize();

        _particleOnThrownQuery = GetEntityQuery<ParticleOnThrownComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<ParticleOnGunShotProjectileComponent, AmmoShotEvent>(OnGunShotProjectile);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var orchestras in _thrownOrchestras.Values)
        {
            foreach (var orchestra in orchestras)
            {
                _orchestra.Stop(orchestra);
            }
        }

        _thrownOrchestras.Clear();
        _staleThrownOrchestras.Clear();
    }

    private void OnGunShotProjectile(Entity<ParticleOnGunShotProjectileComponent> ent, ref AmmoShotEvent args)
    {
        // Выстрел создаёт одноразовые слои и при replay не должен дублировать их на том же клиенте.
        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            foreach (var specifier in ent.Comp.Orchestras)
            {
                _orchestra.Start(specifier, projectile);
            }
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<ParticleOnThrownComponent, ThrownItemComponent>();
        while (query.MoveNext(out var uid, out var particleOnThrown, out var thrownItem))
        {
            if (thrownItem.Landed)
            {
                StopThrownOrchestras(uid);
                continue;
            }

            StartThrownOrchestras((uid, particleOnThrown));
        }

        _staleThrownOrchestras.Clear();
        foreach (var uid in _thrownOrchestras.Keys)
        {
            if (!_particleOnThrownQuery.HasComp(uid) ||
                !_thrownItemQuery.TryComp(uid, out var thrownItem) ||
                thrownItem.Landed)
            {
                _staleThrownOrchestras.Add(uid);
            }
        }

        foreach (var uid in _staleThrownOrchestras)
        {
            StopThrownOrchestras(uid);
        }
    }

    private void StartThrownOrchestras(Entity<ParticleOnThrownComponent> ent)
    {
        if (_thrownOrchestras.ContainsKey(ent))
            return;

        List<ActiveParticleOrchestra>? active = null;
        foreach (var specifier in ent.Comp.Orchestras)
        {
            var orchestra = _orchestra.Start(specifier, ent);
            if (orchestra == null)
                continue;

            active ??= new List<ActiveParticleOrchestra>(ent.Comp.Orchestras.Count);
            active.Add(orchestra);
        }

        if (active != null)
            _thrownOrchestras[ent] = active;
    }

    private void StopThrownOrchestras(EntityUid uid)
    {
        if (!_thrownOrchestras.Remove(uid, out var orchestras))
            return;

        foreach (var orchestra in orchestras)
        {
            _orchestra.Stop(orchestra);
        }
    }
}
