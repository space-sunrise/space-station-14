using System.Numerics;
using Content.Server.Stunnable;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared._Sunrise.Weapons.Melee.Events;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Sunrise.Weapons.Melee.Systems;

/// <summary>
/// Applies landing-tile knockdowns after a melee blink finishes.
/// </summary>
public sealed class MeleeBlinkKnockdownSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeBlinkKnockdownComponent, MeleeBlinkLandedEvent>(OnBlinkLanded);
    }

    private void OnBlinkLanded(Entity<MeleeBlinkKnockdownComponent> ent, ref MeleeBlinkLandedEvent args)
    {
        EntityUid? closestTarget = null;
        var closestDistance = float.MaxValue;
        var landing = Transform(args.User).MapPosition.Position;

        foreach (var entity in _lookup.GetEntitiesInRange(args.Coordinates, ent.Comp.Radius, LookupFlags.Dynamic | LookupFlags.Approximate))
        {
            if (entity == args.User)
                continue;

            if (!_mobState.IsAlive(entity))
                continue;

            var distance = Vector2.DistanceSquared(Transform(entity).MapPosition.Position, landing);
            if (distance >= closestDistance)
                continue;

            closestTarget = entity;
            closestDistance = distance;
        }

        if (closestTarget is { } target)
            _stun.TryKnockdown(target, ent.Comp.KnockdownDuration, force: true);
    }
}
