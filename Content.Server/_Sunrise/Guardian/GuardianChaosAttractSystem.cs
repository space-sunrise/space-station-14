using System.Numerics;
using Content.Server.Guardian;
using Content.Shared._Sunrise.Guardian;
using Content.Shared.Timing;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;

namespace Content.Server._Sunrise.Guardian;

public sealed class GuardianChaosAttractSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    private readonly HashSet<EntityUid> _targets = new();
    private EntityQuery<GuardianComponent> _guardianQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _guardianQuery = GetEntityQuery<GuardianComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<GuardianChaosAttractComponent, MeleeHitEvent>(
            OnMeleeHit,
            before: [typeof(UseDelayOnMeleeHitSystem)]);
    }

    private void OnMeleeHit(Entity<GuardianChaosAttractComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || _delay.IsDelayed(ent.Owner))
            return;

        TryAttract(ent.AsNullable());
    }

    public bool TryAttract(Entity<GuardianChaosAttractComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var coordinates = _transform.GetMapCoordinates(ent.Owner);
        Spawn(ent.Comp.Effect, coordinates);

        var host = _guardianQuery.TryComp(ent.Owner, out var guardian)
            ? guardian.Host
            : null;

        _targets.Clear();
        var epicenter = coordinates.Position;
        _lookup.GetEntitiesInRange(
            coordinates.MapId,
            epicenter,
            ent.Comp.Range,
            _targets,
            flags: LookupFlags.Dynamic | LookupFlags.Sundries);

        foreach (var target in _targets)
        {
            if (target == ent.Owner || target == host)
                continue;

            if (!_physicsQuery.TryComp(target, out var physics) ||
                (physics.CollisionLayer & (int) ent.Comp.CollisionMask) != 0x0)
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, target))
                continue;

            var targetPos = _transform.GetWorldPosition(target);
            var direction = targetPos - epicenter;

            if (direction == Vector2.Zero)
                continue;

            var throwDirection = ent.Comp.Speed < 0
                ? -direction
                : direction.Normalized() * (ent.Comp.Range - direction.Length());

            _throwing.TryThrow(target, throwDirection, Math.Abs(ent.Comp.Speed), ent.Owner, recoil: false, compensateFriction: true);
        }

        return true;
    }
}
