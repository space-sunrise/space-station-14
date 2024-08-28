using System.Numerics;
using System.Threading;
using Content.Server._Sunrise.DontSellingGrid;
using Content.Server._Sunrise.ImmortalGrid;
using Content.Server._Sunrise.NightDayMapLight;
using Content.Server._Sunrise.TransitHub;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Pinpointer;
using Content.Server.Parallax;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.GameTicking;
using Content.Shared.Localizations;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.GameTicking;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Audio;

namespace Content.Server.Shuttles.Systems;

public sealed partial class EmergencyShuttleSystem : EntitySystem
{
    /*
     * Handles the escape shuttle + CentCom.
     */

    [Dependency] private readonly IAdminLogManager _logger = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly AccessReaderSystem _reader = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly CommunicationsConsoleSystem _commsConsole = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly DockingSystem _dock = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IdCardSystem _idSystem = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly BiomeSystem _biomes = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;

    private const float ShuttleSpawnBuffer = 1f;

    // Sunrise-start
    public TimeSpan? DockTime;
    // Sunrise-end

    private bool _emergencyShuttleEnabled;

    [ValidatePrototypeId<TagPrototype>]
    private const string DockTag = "DockEmergency";

    public override void Initialize()
    {
        _emergencyShuttleEnabled = _configManager.GetCVar(CCVars.EmergencyShuttleEnabled);
        // Don't immediately invoke as roundstart will just handle it.
        Subs.CVar(_configManager, CCVars.EmergencyShuttleEnabled, SetEmergencyShuttleEnabled);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<StationEmergencyShuttleComponent, StationPostInitEvent>(OnStationStartup);
        SubscribeLocalEvent<StationTransitHubComponent, ComponentShutdown>(OnCentcommShutdown); // Sunrise-Edit
        SubscribeLocalEvent<StationTransitHubComponent, ComponentInit>(OnTransitHubInit); // Sunrise-Edit

        SubscribeLocalEvent<EmergencyShuttleComponent, FTLStartedEvent>(OnEmergencyFTL);
        SubscribeLocalEvent<EmergencyShuttleComponent, FTLCompletedEvent>(OnEmergencyFTLComplete);
        SubscribeNetworkEvent<EmergencyShuttleRequestPositionMessage>(OnShuttleRequestPosition);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded); // Sunrise-edit
        InitializeEmergencyConsole();
    }

    // Sunrise-start
    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        DockTime = null;
    }
    // Sunrise-end

    private void OnRoundStart(RoundStartingEvent ev)
    {
        CleanupEmergencyConsole();
        _roundEndCancelToken = new CancellationTokenSource();
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        _roundEndCancelToken?.Cancel();
        _roundEndCancelToken = null;
    }

    private void OnCentcommShutdown(EntityUid uid, StationTransitHubComponent component, ComponentShutdown args) // Sunrise-Edit
    {
        ClearTransitHub(component);
    }

    private void ClearTransitHub(StationTransitHubComponent component) // Sunrise-Edit
    {
        QueueDel(component.Entity);
        QueueDel(component.MapEntity);
        component.Entity = null;
        component.MapEntity = null;
    }

    /// <summary>
    ///     Attempts to get the EntityUid of the emergency shuttle
    /// </summary>
    public EntityUid? GetShuttle()
    {
        AllEntityQuery<EmergencyShuttleComponent>().MoveNext(out var shuttle, out _);
        return shuttle;
    }

    private void SetEmergencyShuttleEnabled(bool value)
    {
        if (_emergencyShuttleEnabled == value)
            return;

        _emergencyShuttleEnabled = value;

        if (value)
        {
            SetupEmergencyShuttle();
        }
        else
        {
            CleanupEmergencyShuttle();
        }
    }

    private void CleanupEmergencyShuttle()
    {
        var query = AllEntityQuery<StationTransitHubComponent>(); // Sunrise-Edit

        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<StationTransitHubComponent>(uid); // Sunrise-Edit
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateEmergencyConsole(frameTime);
    }

    /// <summary>
    ///     If the client is requesting debug info on where an emergency shuttle would dock.
    /// </summary>
    private void OnShuttleRequestPosition(EmergencyShuttleRequestPositionMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.IsAdmin(args.SenderSession))
            return;

        var player = args.SenderSession.AttachedEntity;
        if (player is null)
            return;

        var station = _station.GetOwningStation(player.Value);

        if (!TryComp<StationEmergencyShuttleComponent>(station, out var stationShuttle) ||
            !HasComp<ShuttleComponent>(stationShuttle.EmergencyShuttle))
        {
            return;
        }

        var targetGrid = _station.GetLargestGrid(Comp<StationDataComponent>(station.Value));
        if (targetGrid == null)
            return;

        var config = _dock.GetDockingConfig(stationShuttle.EmergencyShuttle.Value, targetGrid.Value, DockTag, true);
        if (config == null)
            return;

        foreach (var configDock in config.Docks)
        {
            _dock.Undock((configDock.DockBUid, configDock.DockB));
        }

        RaiseNetworkEvent(new EmergencyShuttlePositionMessage()
        {
            StationUid = GetNetEntity(targetGrid),
            Position = config.Area,
        });
    }

    /// <summary>
    ///     Escape shuttle FTL event handler. The only escape shuttle FTL transit should be from station to centcomm at round end
    /// </summary>
    private void OnEmergencyFTL(EntityUid uid, EmergencyShuttleComponent component, ref FTLStartedEvent args)
    {
        var ftlTime = TimeSpan.FromSeconds
        (
            TryComp<FTLComponent>(uid, out var ftlComp) ? ftlComp.TravelTime : _shuttle.DefaultTravelTime
        );

        if (TryComp<DeviceNetworkComponent>(uid, out var netComp))
        {
            var payload = new NetworkPayload
            {
                [ShuttleTimerMasks.ShuttleMap] = uid,
                [ShuttleTimerMasks.SourceMap] = args.FromMapUid,
                [ShuttleTimerMasks.DestMap] = _transformSystem.GetMap(args.TargetCoordinates),
                [ShuttleTimerMasks.ShuttleTime] = ftlTime,
                [ShuttleTimerMasks.SourceTime] = ftlTime,
                [ShuttleTimerMasks.DestTime] = ftlTime
            };
            _deviceNetworkSystem.QueuePacket(uid, null, payload, netComp.TransmitFrequency);
        }
    }

    /// <summary>
    ///     When the escape shuttle finishes FTL (docks at centcomm), have the timers display the round end countdown
    /// </summary>
    private void OnEmergencyFTLComplete(EntityUid uid, EmergencyShuttleComponent component, ref FTLCompletedEvent args)
    {
        var countdownTime = TimeSpan.FromSeconds(_configManager.GetCVar(CCVars.RoundRestartTime));
        var shuttle = args.Entity;
        if (TryComp<DeviceNetworkComponent>(shuttle, out var net))
        {
            var payload = new NetworkPayload
            {
                [ShuttleTimerMasks.ShuttleMap] = shuttle,
                [ShuttleTimerMasks.SourceMap] = _roundEnd.GetTransitHub(), // Sunrise-Edit
                [ShuttleTimerMasks.DestMap] = _roundEnd.GetStation(),
                [ShuttleTimerMasks.ShuttleTime] = countdownTime,
                [ShuttleTimerMasks.SourceTime] = countdownTime,
                [ShuttleTimerMasks.DestTime] = countdownTime,
            };

            // by popular request
            // https://discord.com/channels/310555209753690112/770682801607278632/1189989482234126356
            if (_random.Next(1000) == 0)
            {
                payload.Add(ScreenMasks.Text, ShuttleTimerMasks.Kill);
                payload.Add(ScreenMasks.Color, Color.Red);
            }
            else
                payload.Add(ScreenMasks.Text, ShuttleTimerMasks.Bye);

            _deviceNetworkSystem.QueuePacket(shuttle, null, payload, net.TransmitFrequency);
        }
    }

    /// <summary>
    ///     Attempts to dock the emergency shuttle to the station.
    /// </summary>
    public void CallEmergencyShuttle(EntityUid stationUid, StationEmergencyShuttleComponent? stationShuttle = null)
    {
        if (!Resolve(stationUid, ref stationShuttle))
            return;

        if (!TryComp<TransformComponent>(stationShuttle.EmergencyShuttle, out var xform) || // Sunrise-Edit
            !TryComp<ShuttleComponent>(stationShuttle.EmergencyShuttle, out var shuttle))
        {
            Log.Error($"Attempted to call an emergency shuttle for an uninitialized station? Station: {ToPrettyString(stationUid)}. Shuttle: {ToPrettyString(stationShuttle.EmergencyShuttle)}");
            return;
        }

        var targetGrid = _station.GetLargestGrid(Comp<StationDataComponent>(stationUid));
        var announcementSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

        // Sunrise-start
        DockTime = _timing.CurTime;
        // Sunrise-end

        // UHH GOOD LUCK
        if (targetGrid == null)
        {
            _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid)} unable to dock with station {ToPrettyString(stationUid)}");
            _chatSystem.DispatchStationAnnouncement(stationUid, Loc.GetString("emergency-shuttle-good-luck"), announcementSound: announcementSound); // Sunrise-edit
            // TODO: Need filter extensions or something don't blame me.
            return;
        }

        var xformQuery = GetEntityQuery<TransformComponent>();

        if (_shuttle.TryFTLDock(stationShuttle.EmergencyShuttle.Value, shuttle, targetGrid.Value, DockTag, true, true)) // Sunrise-Edit
        {
            if (TryComp(targetGrid.Value, out TransformComponent? targetXform))
            {
                var angle = _dock.GetAngle(stationShuttle.EmergencyShuttle.Value, xform, targetGrid.Value, targetXform, xformQuery);
                var direction = ContentLocalizationManager.FormatDirection(angle.GetDir());
                var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((stationShuttle.EmergencyShuttle.Value, xform)));
                _chatSystem.DispatchStationAnnouncement(stationUid, Loc.GetString("emergency-shuttle-docked", ("time", $"{_consoleAccumulator:0}"), ("direction", direction), ("location", location)), playDefault: false);
            }

            // shuttle timers
            var time = TimeSpan.FromSeconds(_consoleAccumulator);
            if (TryComp<DeviceNetworkComponent>(stationShuttle.EmergencyShuttle.Value, out var netComp))
            {
                var payload = new NetworkPayload
                {
                    [ShuttleTimerMasks.ShuttleMap] = stationShuttle.EmergencyShuttle.Value,
                    [ShuttleTimerMasks.SourceMap] = targetXform?.MapUid,
                    [ShuttleTimerMasks.DestMap] = _roundEnd.GetTransitHub(), // Sunrise-Edit
                    [ShuttleTimerMasks.ShuttleTime] = time,
                    [ShuttleTimerMasks.SourceTime] = time,
                    [ShuttleTimerMasks.DestTime] = time + TimeSpan.FromSeconds(TransitTime),
                    [ShuttleTimerMasks.Docked] = true
                };
                _deviceNetworkSystem.QueuePacket(stationShuttle.EmergencyShuttle.Value, null, payload, netComp.TransmitFrequency);
            }

            _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid)} docked with stations");
        }
        else
        {
            if (TryComp<TransformComponent>(targetGrid.Value, out var targetXform))
            {
                var angle = _dock.GetAngle(stationShuttle.EmergencyShuttle.Value, xform, targetGrid.Value, targetXform, xformQuery);
                var direction = ContentLocalizationManager.FormatDirection(angle.GetDir());
                var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((stationShuttle.EmergencyShuttle.Value, xform)));
                _chatSystem.DispatchStationAnnouncement(stationUid, Loc.GetString("emergency-shuttle-nearby", ("time", $"{_consoleAccumulator:0}"), ("direction", direction), ("location", location)), playDefault: false, announcementSound: announcementSound); // Sunrise-Edit
            }

            _logger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Emergency shuttle {ToPrettyString(stationUid)} unable to find a valid docking port for {ToPrettyString(stationUid)}");
            // TODO: Need filter extensions or something don't blame me.
        }
    }

    private void OnTransitHubInit(EntityUid uid, StationTransitHubComponent component, ComponentInit args) // Sunrise-Edit
    {
        // This is handled on map-init, so that centcomm has finished initializing by the time the StationPostInitEvent
        // gets raised
        if (!_emergencyShuttleEnabled)
            return;

        // Post mapinit? fancy
        if (TryComp<TransformComponent>(component.Entity, out var xform)) // Sunrise-Edit
        {
            component.MapEntity = xform.MapUid;
            return;
        }

        AddTransitHub(uid, component); // Sunrise-Edit
    }

    private void OnStationStartup(Entity<StationEmergencyShuttleComponent> ent, ref StationPostInitEvent args)
    {
        AddEmergencyShuttle((ent, ent));
    }

    /// <summary>
    ///     Spawns the emergency shuttle for each station and starts the countdown until controls unlock.
    /// </summary>
    public void CallEmergencyShuttle()
    {
        if (EmergencyShuttleArrived)
            return;

        if (!_emergencyShuttleEnabled)
        {
            _roundEnd.EndRound();
            return;
        }

        _consoleAccumulator = _configManager.GetCVar(CCVars.EmergencyShuttleDockTime);
        EmergencyShuttleArrived = true;

        var query = AllEntityQuery<StationEmergencyShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            CallEmergencyShuttle(uid, comp);
        }

        _commsConsole.UpdateCommsConsoleInterface();
    }

    private void SetupEmergencyShuttle()
    {
        if (!_emergencyShuttleEnabled)
            return;

        var centcommQuery = AllEntityQuery<StationTransitHubComponent>(); // Sunrise-Edit

        while (centcommQuery.MoveNext(out var uid, out var centcomm))
        {
            AddTransitHub(uid, centcomm); // Sunrise-Edit
        }

        var query = AllEntityQuery<StationEmergencyShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            AddEmergencyShuttle((uid, comp));
        }
    }

    private void AddTransitHub(EntityUid station, StationTransitHubComponent component) // Sunrise-Edit
    {
        DebugTools.Assert(LifeStage(station) >= EntityLifeStage.MapInitialized);
        if (component.MapEntity != null || component.Entity != null)
        {
            Log.Warning("Attempted to re-add an existing centcomm map.");
            return;
        }

        // Check for existing centcomms and just point to that
        var query = AllEntityQuery<StationTransitHubComponent>(); // Sunrise-Edit
        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            if (!Exists(otherComp.MapEntity) || !Exists(otherComp.Entity))
            {
                Log.Error($"Discovered invalid centcomm component?");
                ClearTransitHub(otherComp);
                continue;
            }

            component.MapEntity = otherComp.MapEntity;
            component.Entity = otherComp.Entity;
            return;
        }

        if (string.IsNullOrEmpty(component.Map.ToString()))
        {
            Log.Warning("No CentComm map found, skipping setup.");
            return;
        }

        // Sunrise-start
        var mapUid = _mapSystem.CreateMap(out var mapId, runMapInit: false);

        if (!_loader.TryLoad(mapId, component.Map.ToString(), out var uids) || uids.Count != 1)
        {
            Log.Error($"Failed to set up transit hub map!");
            QueueDel(mapUid);
            return;
        }

        EnsureComp<NightDayMapLightComponent>(mapUid);

        Log.Info($"Created transit hub grid {ToPrettyString(uids[0])} on map {ToPrettyString(mapUid)} for station {ToPrettyString(station)}");

        EnsureComp<ProtectedGridComponent>(uids[0]);
        var template = _random.Pick(component.Biomes);
        _biomes.EnsurePlanet(mapUid, _protoManager.Index<BiomeTemplatePrototype>(template), mapLight: component.PlanetLightColor);

        component.MapEntity = mapUid;
        component.Entity = uids[0];

        _mapManager.DoMapInitialize(mapId);
        // Sunrise-end
    }

    // Sunrise-start
    public HashSet<EntityUid> GetTransitHubMaps()
    {
        var query = AllEntityQuery<StationTransitHubComponent>();
        var maps = new HashSet<EntityUid>(Count<StationTransitHubComponent>());

        while (query.MoveNext(out var comp))
        {
            if (comp.MapEntity != null)
                maps.Add(comp.MapEntity.Value);
        }

        return maps;
    }
    // Sunrise-end

    private void AddEmergencyShuttle(Entity<StationEmergencyShuttleComponent?, StationTransitHubComponent?> ent) // Sunrise-edit
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        if (!_emergencyShuttleEnabled)
            return;

        if (ent.Comp1.EmergencyShuttle != null)
        {
            if (Exists(ent.Comp1.EmergencyShuttle))
            {
                Log.Error($"Attempted to add an emergency shuttle to {ToPrettyString(ent)}, despite a shuttle already existing?");
                return;
            }

            Log.Error($"Encountered deleted emergency shuttle during initialization of {ToPrettyString(ent)}");
            ent.Comp1.EmergencyShuttle = null;
        }

        if (!TryComp(ent.Comp2.MapEntity, out MapComponent? map))
        {
            Log.Error($"Failed to add emergency shuttle - transit hub has not been initialized? {ToPrettyString(ent)}");
            return;
        }

        // Load escape shuttle
        var shuttlePath = ent.Comp1.EmergencyShuttlePath;

        // Sunrise-start
        var mapId = _mapManager.CreateMap();

        var mapOptions = new MapLoadOptions { LoadMap = false,};
        if (!_loader.TryLoad(mapId, shuttlePath.ToString(), out var uids, mapOptions) || uids.Count != 1)
        {
            Log.Error($"Unable to spawn emergency shuttle {shuttlePath} for {ToPrettyString(ent)}");
            return;
        }

        var shuttle = uids[0];

        ent.Comp1.EmergencyShuttle = shuttle;
        EnsureComp<ProtectedGridComponent>(shuttle);
        EnsureComp<PreventPilotComponent>(shuttle);
        EnsureComp<EmergencyShuttleComponent>(shuttle);

        // Sunrise-end
        Log.Info($"Added emergency shuttle {ToPrettyString(shuttle)} for station {ToPrettyString(ent)} and centcomm {ToPrettyString(ent.Comp2.Entity)}");
    }

    /// <summary>
    /// Returns whether a target is escaping on the emergency shuttle, but only if evac has arrived.
    /// </summary>
    public bool IsTargetEscaping(EntityUid target)
    {
        // if evac isn't here then sitting in a pod doesn't return true
        if (!EmergencyShuttleArrived)
            return false;

        // check each emergency shuttle
        var xform = Transform(target);
        foreach (var stationData in EntityQuery<StationEmergencyShuttleComponent>())
        {
            if (stationData.EmergencyShuttle == null)
                continue;

            if (IsOnGrid(xform, stationData.EmergencyShuttle.Value))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOnGrid(TransformComponent xform, EntityUid shuttle, MapGridComponent? grid = null, TransformComponent? shuttleXform = null)
    {
        if (!Resolve(shuttle, ref grid, ref shuttleXform))
            return false;

        return _transformSystem.GetWorldMatrix(shuttleXform).TransformBox(grid.LocalAABB).Contains(_transformSystem.GetWorldPosition(xform));
    }
}
