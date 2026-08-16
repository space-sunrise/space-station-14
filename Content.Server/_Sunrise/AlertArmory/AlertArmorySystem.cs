using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Robust.Shared.Audio.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using Content.Shared.Station.Components;
using Robust.Shared.Physics.Components;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Popups;
using Content.Shared.Mobs.Components;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.AlertArmory;

/// <summary>
/// Preloads alert armories and moves them between armory space and their station.
/// </summary>
public sealed partial class AlertArmorySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _nav = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<PendingClockInComponent> _pendingQuery;
    private EntityQuery<ArrivalsBlacklistComponent> _blacklistQuery;
    private EntityQuery<MobStateComponent> _mobQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<AlertArmoryStationComponent, StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<AlertArmoryStationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, FTLStartedEvent>(OnFTLStart);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, FTLTagEvent>(SetShuttleTag);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, FTLCompletedEvent>(AnnounceShuttleDocking);

        _pendingQuery = GetEntityQuery<PendingClockInComponent>();
        _blacklistQuery = GetEntityQuery<ArrivalsBlacklistComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
    }

    private void OnStationPostInit(Entity<AlertArmoryStationComponent> ent, ref StationPostInitEvent args)
    {
        TryInitializeArmories(ent.AsNullable());
    }

    /// <summary>
    /// Preloads every configured armory shuttle for a station.
    /// </summary>
    public bool TryInitializeArmories(Entity<AlertArmoryStationComponent?> ent)
    {
        if (!CanInitializeArmories(ent))
            return false;

        Resolve(ent, ref ent.Comp);
        DoInitializeArmories((ent.Owner, ent.Comp!));
        return true;
    }

    /// <summary>
    /// Checks whether armory shuttles can be initialized for a station.
    /// </summary>
    public bool CanInitializeArmories(Entity<AlertArmoryStationComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return _cfg.GetCVar(CCVars.GridFill) && ent.Comp.Grids.Count == 0;
    }

    private void DoInitializeArmories(Entity<AlertArmoryStationComponent> ent)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        if (!_cfg.GetCVar(CCVars.GridFill))
            return;

        var map = _map.CreateMap(out var mapId);
        _meta.SetEntityName(map, $"AlertArmories {uid}");

        var xOffset = 0f;
        foreach (var (alert, armory) in comp.Shuttles)
        {
            if (!_loader.TryLoadGrid(mapId, armory.Shuttle, out var grid))
            {
                Log.Error($"Failed to load {alert} armory {armory.Shuttle}");
                continue;
            }

            var (gridUid, mapGrid) = grid.Value;

            if (!TryComp<PhysicsComponent>(gridUid, out var physics))
                continue;

            xOffset += mapGrid.LocalAABB.Width / 2;

            var coords = new Vector2(-physics.LocalCenter.X + xOffset, -physics.LocalCenter.Y);
            var eCoords = new EntityCoordinates(map, coords);
            _transform.SetCoordinates(gridUid, eCoords);

            xOffset += (mapGrid.LocalAABB.Width / 2) + 1;

            var armoryComp = EnsureComp<AlertArmoryShuttleComponent>(gridUid);
            armoryComp.Station = uid;
            armoryComp.Announcement = armory.Announcement;
            armoryComp.AnnouncementColor = armory.AnnouncementColor;
            armoryComp.RecallAnnouncement = armory.RecallAnnouncement;
            armoryComp.RecallAnnouncementColor = armory.RecallAnnouncementColor;
            armoryComp.CoordsCache = eCoords;
            armoryComp.ArmorySpaceUid = map;

            comp.Grids[alert] = gridUid;
        }
    }

    private void OnStartup(Entity<AlertArmoryShuttleComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<PreventPilotComponent>(ent);
    }

    private void OnShutdown(Entity<AlertArmoryStationComponent> ent, ref ComponentShutdown args)
    {
        foreach (var grid in ent.Comp.Grids.Values)
            QueueDel(grid);
    }

    private void OnFTLStart(Entity<AlertArmoryShuttleComponent> ent, ref FTLStartedEvent ev)
    {
        if (ev.FromMapUid != ent.Comp.ArmorySpaceUid)
        {
            DumpChildren(ent, ref ev);

            var xform = Transform(ent);
            var location = FormattedMessage.RemoveMarkupPermissive(_nav.GetNearestBeaconString((ent, xform)));
            var station = MetaData(ent.Comp.Station).EntityName;

            if (ent.Comp.RecallAnnouncement != null)
            {
                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString(ent.Comp.RecallAnnouncement, [("location", location), ("station", station)]),
                    colorOverride: ent.Comp.RecallAnnouncementColor ?? Color.PaleVioletRed);
            }
        }

        ent.Comp.InTransit = true;
    }

    private void SetShuttleTag(Entity<AlertArmoryShuttleComponent> ent, ref FTLTagEvent ev)
    {
        if (ev.Handled || ent.Comp.DockTag == null)
            return;

        ev.Handled = true;
        ev.Tag = ent.Comp.DockTag;
    }

    private void AnnounceShuttleDocking(Entity<AlertArmoryShuttleComponent> ent, ref FTLCompletedEvent ev)
    {
        ent.Comp.InTransit = false;

        var xform = Transform(ent);
        var location = FormattedMessage.RemoveMarkupPermissive(_nav.GetNearestBeaconString((ent, xform)));
        var station = MetaData(ent.Comp.Station).EntityName;

        if (ev.MapUid != ent.Comp.ArmorySpaceUid && ent.Comp.Announcement != null)
        {
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString(ent.Comp.Announcement.Value, [("location", location), ("station", station)]),
                colorOverride: ent.Comp.AnnouncementColor ?? Color.PaleVioletRed);
        }
    }

    /// <summary>
    /// Attempts to send an armory shuttle to the station.
    /// </summary>
    public bool TrySendArmory(Entity<AlertArmoryStationComponent?> station, string armoryKey)
    {
        if (!CanSendArmory(station, armoryKey, out var shuttle, out var targetGrid))
            return false;

        DoSendArmory(shuttle, targetGrid);
        return true;
    }

    /// <summary>
    /// Checks whether an armory shuttle can be sent to the station.
    /// </summary>
    public bool CanSendArmory(Entity<AlertArmoryStationComponent?> station,
        string armoryKey,
        out EntityUid shuttle,
        out EntityUid targetGrid)
    {
        shuttle = default;
        targetGrid = default;
        if (!Resolve(station, ref station.Comp) ||
            !station.Comp.Grids.TryGetValue(armoryKey, out shuttle) ||
            !TryComp<StationDataComponent>(station, out var stationData))
            return false;

        var largestGrid = _station.GetLargestGrid((station.Owner, stationData));
        if (largestGrid == null)
            return false;

        targetGrid = largestGrid.Value;
        return true;
    }

    private void DoSendArmory(EntityUid shuttle, EntityUid targetGrid)
    {
        _shuttles.FTLToDock(
            shuttle,
            Comp<ShuttleComponent>(shuttle),
            targetGrid,
            priorityTag: Comp<AlertArmoryShuttleComponent>(shuttle).DockTag);
    }

    /// <summary>
    /// Attempts to recall an armory shuttle to its preload map.
    /// </summary>
    public bool TryRecallArmory(Entity<AlertArmoryStationComponent?> station, string armoryKey)
    {
        if (!CanRecallArmory(station, armoryKey, out var shuttle, out var shuttleComp))
            return false;

        DoRecallArmory((shuttle, shuttleComp));
        return true;
    }

    /// <summary>
    /// Checks whether an armory shuttle can be recalled.
    /// </summary>
    public bool CanRecallArmory(Entity<AlertArmoryStationComponent?> station,
        string armoryKey,
        out EntityUid shuttle,
        out AlertArmoryShuttleComponent shuttleComp)
    {
        shuttle = default;
        shuttleComp = default!;
        if (!Resolve(station, ref station.Comp) ||
            !station.Comp.Grids.TryGetValue(armoryKey, out shuttle))
            return false;

        if (!TryComp<AlertArmoryShuttleComponent>(shuttle, out var resolvedShuttle))
            return false;

        shuttleComp = resolvedShuttle;
        var xform = Transform(shuttle);
        return xform.MapUid != shuttleComp.ArmorySpaceUid;
    }

    private void DoRecallArmory(Entity<AlertArmoryShuttleComponent> shuttle)
    {
        if (TryComp<FTLComponent>(shuttle, out var ftl))
        {
            _audio.Stop(ftl.StartupStream);
            _audio.Stop(ftl.TravelStream);
            RemComp<FTLComponent>(shuttle);
        }

        _shuttles.FTLToCoordinates(
            shuttle,
            Comp<ShuttleComponent>(shuttle),
            shuttle.Comp.CoordsCache,
            0);
    }

    private void DumpChildren(EntityUid uid, ref FTLStartedEvent args)
    {
        var toDump = new List<Entity<TransformComponent>>();
        FindDumpChildren(uid, toDump);
        foreach (var (ent, xform) in toDump)
        {
            var rotation = xform.LocalRotation;
            _transform.SetCoordinates(ent, new EntityCoordinates(args.FromMapUid!.Value, Vector2.Transform(xform.LocalPosition, args.FTLFrom)));
            _transform.SetWorldRotation(ent, args.FromRotation + rotation);
            _popup.PopupEntity(Loc.GetString("latejoin-arrivals-dumped-from-shuttle"), ent);
        }
    }

    private void FindDumpChildren(EntityUid uid, List<Entity<TransformComponent>> toDump)
    {
        if (_pendingQuery.HasComponent(uid))
            return;

        var xform = Transform(uid);

        if (_mobQuery.HasComponent(uid) || _blacklistQuery.HasComponent(uid))
        {
            toDump.Add((uid, xform));
            return;
        }

        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            FindDumpChildren(child, toDump);
        }
    }
}
