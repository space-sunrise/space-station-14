using System.Numerics;
using Content.Server.Charges;
using Content.Server.Popups;
using Content.Shared._Sunrise.Biocode;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared._Sunrise.Weapons.Melee.Events;
using Content.Shared.Charges.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Weapons.Melee.Systems;

public sealed class MeleeBlinkSystem : EntitySystem
{
    [Dependency] private readonly BiocodeSystem _biocode = default!;
    [Dependency] private readonly ChargesSystem _charges = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private const string SourceEffectPrototype = "TeleportEffectSource";
    private const string TargetEffectPrototype = "TeleportEffectTarget";
    private const string TeleportDelayId = "TeleportDelay";

    private const int MaxCorrectionTries = 16;
    private const int MaxCorrectionRadius = 4;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeBlinkComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<MeleeBlinkComponent, GetVerbsEvent<ActivationVerb>>(OnGetActivationVerb);
    }

    private void OnGetAlternativeVerb(Entity<MeleeBlinkComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(CreateBlinkVerb<AlternativeVerb>(ent, args.User));
    }

    private void OnGetActivationVerb(Entity<MeleeBlinkComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(CreateBlinkVerb<ActivationVerb>(ent, args.User));
    }

    private T CreateBlinkVerb<T>(Entity<MeleeBlinkComponent> ent, EntityUid user) where T : Verb, new()
    {
        var canBlink = CanBlink(ent, user, out var disabledMessage, quiet: true);
        return new T
        {
            Text = Loc.GetString("syndicate-teleporter-verb"),
            Disabled = !canBlink,
            Message = disabledMessage,
            Act = () => TryBlink(ent, user, quiet: false),
        };
    }

    private bool TryBlink(Entity<MeleeBlinkComponent> ent, EntityUid user, bool quiet = false)
    {
        if (!CanBlink(ent, user, out _, quiet))
            return false;

        DoBlink(ent, user);
        return true;
    }

    private bool CanBlink(Entity<MeleeBlinkComponent> ent, EntityUid user, out string? disabledMessage, bool quiet = false)
    {
        disabledMessage = null;

        if (!IsCarriedByUser(ent.Owner, user))
        {
            disabledMessage = Loc.GetString("syndicate-teleporter-not-on-user");
            if (!quiet)
                _popup.PopupEntity(disabledMessage, user, user);

            return false;
        }

        if (TryComp<BiocodeComponent>(ent.Owner, out var biocode) && !_biocode.CanUse(user, biocode.Factions))
        {
            if (!string.IsNullOrEmpty(biocode.AlertText))
            {
                disabledMessage = Loc.GetString(biocode.AlertText);
                if (!quiet)
                    _popup.PopupEntity(disabledMessage, user, user);
            }

            return false;
        }

        if (TryComp<LimitedChargesComponent>(ent.Owner, out var charges) && _charges.IsEmpty((ent.Owner, charges)))
        {
            disabledMessage = Loc.GetString("syndicate-teleporter-no-charges");
            if (!quiet)
                _popup.PopupEntity(disabledMessage, user, user);

            return false;
        }

        if (TryComp<UseDelayComponent>(ent.Owner, out var useDelay) &&
            _useDelay.IsDelayed((ent.Owner, useDelay), GetTeleportDelayId(useDelay)))
        {
            disabledMessage = Loc.GetString("syndicate-teleporter-on-cooldown");
            if (!quiet)
                _popup.PopupEntity(disabledMessage, user, user);

            return false;
        }

        return true;
    }

    private bool IsCarriedByUser(EntityUid item, EntityUid user)
    {
        if (Transform(item).ParentUid == user)
            return true;

        var current = item;
        while (_container.TryGetContainingContainer(current, out var container))
        {
            if (container.Owner == user)
                return true;

            current = container.Owner;
        }

        return false;
    }

    private void DoBlink(Entity<MeleeBlinkComponent> ent, EntityUid user)
    {
        if (TryComp<UseDelayComponent>(ent.Owner, out var useDelay))
            _useDelay.TryResetDelay((ent.Owner, useDelay), true, GetTeleportDelayId(useDelay));

        if (TryComp<LimitedChargesComponent>(ent.Owner, out var charges))
            _charges.TryUseCharge((ent.Owner, charges));

        Blink(user, ent);
    }

    private void Blink(EntityUid user, Entity<MeleeBlinkComponent> ent)
    {
        var pre = Transform(user).Coordinates;

        var random = ent.Comp.RandomDistanceValue > 0 ? _random.Next(0, ent.Comp.RandomDistanceValue + 1) : 0;
        var dist = ent.Comp.TeleportationValue + random;
        var dir = Transform(user).LocalRotation.ToWorldVec().Normalized();
        var target = pre.Offset(dir * new Vector2(dist, dist));

        Spawn(SourceEffectPrototype, _transform.ToMapCoordinates(pre));

        if (_transform.GetMapId(user) != _transform.GetMapId(target))
            return;

        if (IsSpotFree(user, target))
        {
            ApplyLanding(ent.Owner, user, target, ent.Comp);
            return;
        }

        if (TryFindSafeTile(user, target, out var safe))
        {
            ApplyLanding(ent.Owner, user, safe.Value, ent.Comp);
            ApplyBlockedDamage(user, ent.Comp);
            return;
        }

        _transform.SetCoordinates(user, pre);
        ApplyBlockedDamage(user, ent.Comp);
    }

    private void ApplyBlockedDamage(EntityUid user, MeleeBlinkComponent comp)
    {
        if (comp.DamageOnBlocked is { } dmg && HasComp<DamageableComponent>(user))
            _damage.TryChangeDamage(user, dmg);
    }

    private bool IsSpotFree(EntityUid user, EntityCoordinates coords)
    {
        if (_transform.GetMapId(user) != _transform.GetMapId(coords))
            return false;

        var tile = _turf.GetTileRef(coords);
        if (tile is null || _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            return false;

        foreach (var body in _turf.GetEntitiesInTile(coords, LookupFlags.Static | LookupFlags.Dynamic))
        {
            if (body == user)
                continue;

            if (!IsBlockingEntity(body))
                continue;

            return false;
        }

        return true;
    }

    private bool TryFindSafeTile(EntityUid user, EntityCoordinates origin, out EntityCoordinates? result)
    {
        var mapId = _transform.GetMapId(user);

        foreach (var cand in EnumerateCandidates(origin, mapId, MaxCorrectionRadius))
        {
            if (IsSpotFree(user, cand))
            {
                result = cand;
                return true;
            }
        }

        result = null;
        return false;
    }

    private IEnumerable<EntityCoordinates> EnumerateCandidates(EntityCoordinates origin, MapId mapId, int maxRadius)
    {
        for (var radius = 1; radius <= maxRadius; radius++)
        {
            for (var i = 0; i < MaxCorrectionTries; i++)
            {
                var baseDeg = 45 * _random.Next(0, 8);
                var jitter = _random.Next(-10, 11);
                var angle = Angle.FromDegrees(baseDeg + jitter);

                var step = angle.ToWorldVec() * new Vector2(radius, radius);
                var target = origin.Offset(step);

                if (mapId != _transform.GetMapId(target))
                    continue;

                yield return target;
            }
        }
    }

    private static string GetTeleportDelayId(UseDelayComponent component)
    {
        return component.Delays?.ContainsKey(TeleportDelayId) == true
            ? TeleportDelayId
            : UseDelaySystem.DefaultId;
    }

    private void ApplyLanding(EntityUid weapon, EntityUid user, EntityCoordinates where, MeleeBlinkComponent comp)
    {
        var landing = where;
        if (comp.LandingRandomOffset > 0f)
        {
            var angle = Angle.FromDegrees(_random.Next(0, 360));
            var dist = _random.NextFloat(0f, comp.LandingRandomOffset);
            var offset = angle.ToWorldVec() * dist;
            var candidate = where.Offset(offset);

            if (IsSpotFree(user, candidate))
                landing = candidate;
        }

        _transform.SetCoordinates(user, landing);

        var landed = new MeleeBlinkLandedEvent(user, landing);
        RaiseLocalEvent(weapon, ref landed);

        Spawn(TargetEffectPrototype, _transform.ToMapCoordinates(landing));
    }

    private bool IsBlockingEntity(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics) ||
            !physics.CanCollide ||
            !physics.Hard ||
            (physics.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
        {
            return false;
        }

        return Transform(uid).Anchored;
    }
}
