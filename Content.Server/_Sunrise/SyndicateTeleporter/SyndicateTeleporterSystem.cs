using System.Numerics;
using Content.Server.Popups;
using Content.Shared._Sunrise.Biocode;
using Content.Shared._Sunrise.SyndicateTeleporter;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.SyndicateTeleporter;

public sealed class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BiocodeSystem _biocode = default!;

    private const string SourceEffectPrototype = "TeleportEffectSource";
    private const string TargetEffectPrototype = "TeleportEffectTarget";
    private const string TeleportDelayId = "TeleportDelay";

    private const int MaxCorrectionTries = 16;
    private const int MaxCorrectionRadius = 4;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SyndicateTeleporterComponent, GetVerbsEvent<ActivationVerb>>(OnGetActivationVerb);
    }

    private void OnGetActivationVerb(Entity<SyndicateTeleporterComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var canTeleport = CanTeleport(ent.AsNullable(), user, out var disabledMessage, quiet: true);
        var verb = new ActivationVerb
        {
            Text = Loc.GetString("syndicate-teleporter-verb"),
            Disabled = !canTeleport,
            Message = disabledMessage,
            Act = () => TryTeleport(ent.AsNullable(), user, quiet: false)
        };

        args.Verbs.Add(verb);
    }

    public bool TryTeleport(Entity<SyndicateTeleporterComponent?> ent, EntityUid user, bool quiet = false)
    {
        if (!CanTeleport(ent, user, out _, quiet))
            return false;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        DoTeleport((ent.Owner, ent.Comp), user);
        return true;
    }

    public bool CanTeleport(Entity<SyndicateTeleporterComponent?> ent, EntityUid user, out string? disabledMessage, bool quiet = false)
    {
        disabledMessage = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

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

    private void DoTeleport(Entity<SyndicateTeleporterComponent> ent, EntityUid user)
    {
        if (TryComp<UseDelayComponent>(ent.Owner, out var useDelay))
            _useDelay.TryResetDelay((ent.Owner, useDelay), true, GetTeleportDelayId(useDelay));

        if (TryComp<LimitedChargesComponent>(ent.Owner, out var charges))
            _charges.TryUseCharge((ent.Owner, charges));

        Teleport(user, ent.Comp);
    }

    private void Teleport(EntityUid user, SyndicateTeleporterComponent comp)
    {
        var pre = Transform(user).Coordinates;

        var random = comp.RandomDistanceValue > 0 ? _random.Next(0, comp.RandomDistanceValue + 1) : 0;
        var dist = comp.TeleportationValue + random;
        var dir = Transform(user).LocalRotation.ToWorldVec().Normalized();
        var target = pre.Offset(dir * new Vector2(dist, dist));

        Spawn(SourceEffectPrototype, _transform.ToMapCoordinates(pre));

        if (_transform.GetMapId(user) != _transform.GetMapId(target))
            return;

        // Свободно - нет урона
        if (IsSpotFree(user, target))
        {
            ApplyLanding(user, target);
            return;
        }

        // Стенка - урон
        if (TryFindSafeTile(user, target, out var safe))
        {
            ApplyLanding(user, safe!.Value);
            ApplyBlockedDamage(user, comp);
            return;
        }

        // 0 места для тп, назад
        _transform.SetCoordinates(user, pre);
        ApplyBlockedDamage(user, comp);
    }

    private void ApplyBlockedDamage(EntityUid user, SyndicateTeleporterComponent comp)
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

        foreach (var body in _turf.GetEntitiesInTile(coords, LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (body == user)
                continue;

            if (!Transform(body).Anchored ||
                !TryComp<PhysicsComponent>(body, out var physics) ||
                !physics.CanCollide ||
                !physics.Hard ||
                (physics.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
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
        return component.Delays.ContainsKey(TeleportDelayId)
            ? TeleportDelayId
            : UseDelaySystem.DefaultId;
    }

    private void ApplyLanding(EntityUid user, EntityCoordinates where)
    {
        _transform.SetCoordinates(user, where);
        Spawn(TargetEffectPrototype, _transform.ToMapCoordinates(where));
    }
}
