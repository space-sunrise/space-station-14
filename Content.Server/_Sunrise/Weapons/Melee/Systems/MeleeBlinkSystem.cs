using System.Numerics;
using Content.Server.Charges;
using Content.Server.Popups;
using Content.Shared._Sunrise.Biocode;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared._Sunrise.Weapons.Melee.Events;
using Content.Shared.Charges.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
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

/// <summary>
/// Handles reusable melee blink verbs, cooldowns, charges, and landing resolution for blink-enabled weapons.
/// </summary>
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
        SubscribeAllEvent<RequestMeleeBlinkEvent>(OnRequestMeleeBlink);
        SubscribeLocalEvent<MeleeBlinkComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MeleeBlinkComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<MeleeBlinkComponent, GetVerbsEvent<ActivationVerb>>(OnGetActivationVerb);
    }

    private void OnRequestMeleeBlink(RequestMeleeBlinkEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } user)
            return;

        var weapon = GetEntity(msg.Weapon);
        if (!TryComp<MeleeBlinkComponent>(weapon, out var blink))
            return;

        TryBlink((weapon, blink), user, GetCoordinates(msg.Coordinates), quiet: false);
    }

    private void OnAfterInteract(Entity<MeleeBlinkComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (TryBlink(ent, args.User, args.ClickLocation, quiet: false))
            args.Handled = true;
    }

    private void OnGetAlternativeVerb(Entity<MeleeBlinkComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(CreateBlinkVerb<AlternativeVerb>(ent, args.User, GetForwardTarget(args.User, ent.Comp)));
    }

    private void OnGetActivationVerb(Entity<MeleeBlinkComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(CreateBlinkVerb<ActivationVerb>(ent, args.User, GetForwardTarget(args.User, ent.Comp)));
    }

    private T CreateBlinkVerb<T>(Entity<MeleeBlinkComponent> ent, EntityUid user, EntityCoordinates target) where T : Verb, new()
    {
        var canBlink = CanBlink(ent, user, target, out var disabledMessage, quiet: true);
        var verb = new T
        {
            Text = Loc.GetString("syndicate-teleporter-verb"),
            Disabled = !canBlink,
            Message = disabledMessage,
            Act = () => TryBlink(ent, user, target, quiet: false),
        };

        if (verb is AlternativeVerb alternativeVerb)
            alternativeVerb.Priority = 10;

        return verb;
    }

    private bool TryBlink(Entity<MeleeBlinkComponent> ent, EntityUid user, EntityCoordinates target, bool quiet = false)
    {
        if (!CanBlink(ent, user, target, out _, quiet))
            return false;

        DoBlink(ent, user, target);
        return true;
    }

    private bool CanBlink(Entity<MeleeBlinkComponent> ent, EntityUid user, EntityCoordinates target, out string? disabledMessage, bool quiet = false)
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

        if (!target.IsValid(EntityManager) || _transform.GetMapId(user) != _transform.GetMapId(target))
            return false;

        var userPosition = Transform(user).MapPosition.Position;
        var targetPosition = _transform.ToMapCoordinates(target).Position;
        var maxDistance = ent.Comp.TeleportationValue + ent.Comp.RandomDistanceValue;
        if (Vector2.DistanceSquared(userPosition, targetPosition) > maxDistance * maxDistance)
            return false;

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

    private void DoBlink(Entity<MeleeBlinkComponent> ent, EntityUid user, EntityCoordinates target)
    {
        if (TryComp<UseDelayComponent>(ent.Owner, out var useDelay))
            _useDelay.TryResetDelay((ent.Owner, useDelay), true, GetTeleportDelayId(useDelay));

        if (TryComp<LimitedChargesComponent>(ent.Owner, out var charges))
            _charges.TryUseCharge((ent.Owner, charges));

        Blink(user, ent, target);
    }

    private void Blink(EntityUid user, Entity<MeleeBlinkComponent> ent, EntityCoordinates target)
    {
        var pre = Transform(user).Coordinates;

        Spawn(SourceEffectPrototype, _transform.ToMapCoordinates(pre));

        if (_transform.GetMapId(user) != _transform.GetMapId(target))
            return;

        if (IsSpotFree(user, target))
        {
            ApplyLanding(ent.Owner, user, target);
            return;
        }

        if (TryFindSafeTile(user, target, out var safe))
        {
            ApplyLanding(ent.Owner, user, safe);
            ApplyBlockedDamage(user, ent.Comp);
            return;
        }

        _transform.SetCoordinates(user, pre);
        ApplyBlockedDamage(user, ent.Comp);
    }

    private EntityCoordinates GetForwardTarget(EntityUid user, MeleeBlinkComponent comp)
    {
        var random = comp.RandomDistanceValue > 0 ? _random.Next(0, comp.RandomDistanceValue + 1) : 0;
        var distance = comp.TeleportationValue + random;
        var direction = Transform(user).LocalRotation.ToWorldVec().Normalized();

        return Transform(user).Coordinates.Offset(direction * new Vector2(distance, distance));
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

    private bool TryFindSafeTile(EntityUid user, EntityCoordinates origin, out EntityCoordinates result)
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

        result = default;
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

    private void ApplyLanding(EntityUid weapon, EntityUid user, EntityCoordinates where)
    {
        _transform.SetCoordinates(user, where);

        var landed = new MeleeBlinkLandedEvent(user, where);
        RaiseLocalEvent(weapon, ref landed);

        Spawn(TargetEffectPrototype, _transform.ToMapCoordinates(where));
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
