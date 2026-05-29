using System.Numerics;
using Content.Server.Beam;
using Content.Server.Guardian;
using Content.Shared._Sunrise.Guardian;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Guardian;

public sealed class GuardianLightningArcSystem : EntitySystem
{
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _arcCandidates = new();
    private readonly HashSet<EntityUid> _nearbyCandidates = new();
    private readonly HashSet<EntityUid> _damaged = new();

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<GuardianLightningArcComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GuardianLightningArcComponent, GuardianComponent>();
        while (query.MoveNext(out var uid, out var arc, out var guardian))
        {
            UpdateHostProtection((uid, arc), guardian.Host);

            if (!guardian.GuardianLoose || guardian.Host is not { } host)
                continue;

            var curTime = _timing.CurTime;
            if (arc.NextUpdate > curTime)
                continue;

            arc.NextUpdate = curTime + arc.UpdateInterval;
            ProcessArc((uid, arc), host);
        }
    }

    private void OnShutdown(Entity<GuardianLightningArcComponent> ent, ref ComponentShutdown args)
    {
        RemoveHostProtection(ent.Comp.ProtectedHost, ent.Comp.AddedHostInsulation);
    }

    private void UpdateHostProtection(Entity<GuardianLightningArcComponent> ent, EntityUid? host)
    {
        if (ent.Comp.ProtectedHost == host)
            return;

        RemoveHostProtection(ent.Comp.ProtectedHost, ent.Comp.AddedHostInsulation);
        ent.Comp.ProtectedHost = null;
        ent.Comp.AddedHostInsulation = false;

        if (host == null)
            return;

        if (!HasComp<InsulatedComponent>(host.Value))
        {
            EnsureComp<InsulatedComponent>(host.Value);
            ent.Comp.AddedHostInsulation = true;
        }

        ent.Comp.ProtectedHost = host;
    }

    private void RemoveHostProtection(EntityUid? host, bool addedHostInsulation)
    {
        if (host == null || !addedHostInsulation)
            return;

        RemComp<InsulatedComponent>(host.Value);
    }

    private void ProcessArc(Entity<GuardianLightningArcComponent> ent, EntityUid host)
    {
        if (!_transformQuery.TryComp(ent.Owner, out var guardianXform) ||
            !_transformQuery.TryComp(host, out var hostXform))
        {
            return;
        }

        var guardianCoords = _transform.GetMapCoordinates(ent.Owner, guardianXform);
        var hostCoords = _transform.GetMapCoordinates(host, hostXform);

        if (guardianCoords.MapId != hostCoords.MapId)
            return;

        _beam.TryCreateBeam(ent.Owner, host, ent.Comp.BeamPrototype);

        var start = guardianCoords.Position;
        var end = hostCoords.Position;
        var direction = end - start;
        var length = direction.Length();

        if (length == 0f)
            return;

        _arcCandidates.Clear();
        _damaged.Clear();

        var center = start + direction / 2f;
        _lookup.GetEntitiesInRange(
            guardianCoords.MapId,
            center,
            length / 2f + ent.Comp.ArcWidth + ent.Comp.SplashRadius,
            _arcCandidates,
            flags: LookupFlags.Dynamic);

        foreach (var target in _arcCandidates)
        {
            if (!CanDamage(target, ent.Owner, host))
                continue;

            var targetPos = _transform.GetWorldPosition(target);
            if (!IsInArc(targetPos, start, end, ent.Comp.ArcWidth))
                continue;

            AddDamagedWithNearby(target, ent.Owner, host, ent.Comp.SplashRadius);
        }

        foreach (var target in _damaged)
            _damageable.TryChangeDamage(target, ent.Comp.Damage, ignoreResistances: true, interruptsDoAfters: false, origin: ent.Owner);
    }

    private void AddDamagedWithNearby(EntityUid target, EntityUid guardian, EntityUid host, float radius)
    {
        _damaged.Add(target);

        if (!_transformQuery.TryComp(target, out var xform))
            return;

        _nearbyCandidates.Clear();
        _lookup.GetEntitiesInRange(
            xform.Coordinates,
            radius,
            _nearbyCandidates,
            LookupFlags.Dynamic);

        foreach (var nearby in _nearbyCandidates)
        {
            if (CanDamage(nearby, guardian, host))
                _damaged.Add(nearby);
        }
    }

    private bool CanDamage(EntityUid target, EntityUid guardian, EntityUid host)
    {
        return target != guardian &&
               target != host &&
               _mobStateQuery.HasComp(target) &&
               _damageableQuery.HasComp(target);
    }

    private static bool IsInArc(Vector2 point, Vector2 start, Vector2 end, float width)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();

        if (lengthSquared == 0f)
            return false;

        var projection = Vector2.Dot(point - start, segment) / lengthSquared;

        if (projection is < 0f or > 1f)
            return false;

        var closest = start + projection * segment;
        return Vector2.DistanceSquared(point, closest) <= width * width;
    }
}
