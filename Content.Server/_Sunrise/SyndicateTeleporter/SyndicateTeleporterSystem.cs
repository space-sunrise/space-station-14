using Content.Shared.Interaction.Events;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared._Sunrise.Biocode;
using Content.Server.Popups;
using Robust.Shared.Map;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using System.Numerics;
using System.Threading.Tasks;

namespace Content.Server._Sunrise.SyndicateTeleporter; // В будущем перенести в Shared пофиксив мисспредикт.

public sealed class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BiocodeSystem _biocode = default!;

    private const string SourceEffectPrototype = "TeleportEffectSource";
    private const string TargetEffectPrototype = "TeleportEffectTarget";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SyndicateTeleporterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.InWall == true)
            {
                comp.Timer += frameTime;
                if (comp.Timer >= comp.CorrectTime)
                {
                    SaveTeleport(uid, comp);
                    comp.Timer = 0;
                }
            }
        }
    }

    private void OnUse(EntityUid uid, SyndicateTeleporterComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<BiocodeComponent>(uid, out var biocode) &&
            !_biocode.CanUse(args.User, biocode.Factions))
        {
            if (!string.IsNullOrEmpty(biocode.AlertText))
                _popup.PopupEntity(biocode.AlertText, args.User, args.User);
            args.Handled = true;
            return;
        }

        if (!TryComp<LimitedChargesComponent>(uid, out var charges))
            return;

        if (args.Handled)
            return;

        if (_charges.IsEmpty((uid, charges)))
            return;

        _charges.TryUseCharge((uid, charges));
        component.UserComp = args.User;
        Teleportation(uid, args.User, component);
    }

    private void Teleportation(EntityUid uid, EntityUid user, SyndicateTeleporterComponent comp)
    {
        float random = _random.Next(0, comp.RandomDistanceValue);
        var multiplaer = new Vector2(comp.TeleportationValue + random, comp.TeleportationValue + random); //make random for teleport distance valu

        var transform = Transform(user);
        var offsetValue = transform.LocalRotation.ToWorldVec().Normalized() * multiplaer;
        var coords = transform.Coordinates.Offset(offsetValue); //set coordinates where we move on

        // Spawn source effect at original position
        Spawn(SourceEffectPrototype, Transform(user).Coordinates);

        if (transform.MapID != coords.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(user, coords); // teleport

        // Spawn target effect at new position
        Spawn(TargetEffectPrototype, Transform(user).Coordinates);

        var tile = _turf.GetTileRef(coords); // get info about place where we just teleported. theare a walls?
        if (tile == null)
            return;

        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            comp.InWall = true; // if yes then starting the timer countdown in update
        }
    }

    private void SaveTeleport(EntityUid uid, SyndicateTeleporterComponent comp)
    {
        var transform = Transform(comp.UserComp);
        var offsetValue = Transform(comp.UserComp).LocalPosition;
        var coords = transform.Coordinates.WithPosition(offsetValue);

        var tile = _turf.GetTileRef(coords);
        if (tile == null)
            return;

        var saveattempts = comp.SaveAttempts;
        var savedistance = comp.SaveDistance;

        while (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            if (!TryComp<BodyComponent>(comp.UserComp, out var body))
                return;

            EntityUid? tuser = null;

            if (saveattempts > 0) // if we have chance to survive then teleport in random side away
            {
                double side = _random.Next(-180, 180);
                offsetValue = Angle.FromDegrees(side).ToWorldVec() * savedistance; //averages the resulting direction, turning it into one of 8 directions, (N, NE, E...)
                coords = transform.Coordinates.Offset(offsetValue);
                _transformSystem.SetCoordinates(comp.UserComp, coords);

                // Spawn target effect at corrected position
                Spawn(TargetEffectPrototype, coords);
                _audio.PlayPredicted(comp.AlarmSound, uid, tuser);

                saveattempts--;
            }
            else
            {
                _body.GibBody(comp.UserComp, true, body);
                comp.InWall = false; // closing the countdown in update
                break;
            }

            tile = _turf.GetTileRef(coords);
            if (tile == null)
            {
                return;
            }
            if (!_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            {
                comp.InWall = false;
                return;
            }
        }
    }
}
