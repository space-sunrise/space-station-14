using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Server.Maps;
using Content.Server.Parallax;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.Shuttles;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.PlanetPrison;

// TODO: Рефактор с целью устранения варнингов и перехода системы на более современное API
public sealed class PlanetPrisonStationSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly BiomeSystem _biomeSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly PlayerJoinableMapSystem _playerJoinableMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentShutdown>(OnPrisonShutdown);
        SubscribeLocalEvent<PlayerJoinableMapLobbyJobsPreparingEvent>(OnPlayerJoinableMapLobbyJobsPreparing);

        Log.Level = LogLevel.Info;
    }

    private void OnPrisonShutdown(EntityUid uid, PlanetPrisonStationComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_mapManager.MapExists(component.MapId))
            _mapManager.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
    }

    private void OnPlayerJoinableMapLobbyJobsPreparing(PlayerJoinableMapLobbyJobsPreparingEvent args)
    {
        TryActivatePlanetPrisonForLobbyJobs();
    }

    private void TryActivatePlanetPrisonForLobbyJobs()
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;

        var query = EntityQueryEnumerator<PlanetPrisonStationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            TryActivatePlanetPrison((uid, component), true);
        }
    }

    private bool TryActivatePlanetPrison(Entity<PlanetPrisonStationComponent> ent, bool announceActivation)
    {
        if (ent.Comp.MapId != MapId.Nullspace || ent.Comp.PrisonGrid != EntityUid.Invalid)
            return true;

        if (!TryPickPlanetPrisonMap(ent.Comp, announceActivation, out var gameMap))
            return false;

        var playerCountEnabled = _playerJoinableMap.IsGameMapPlayerCountEnabled(gameMap);
        if (!AddPlanetPrison(ent.Comp, gameMap))
            return false;

        if (announceActivation && playerCountEnabled)
        {
            _chat.DispatchServerAnnouncement(
                Loc.GetString("player-joinable-map-module-activated",
                    ("module", Loc.GetString("player-joinable-map-planet-prison"))),
                Color.LightBlue);
        }

        _stationJobs.UpdateJobsAvailable();
        return true;
    }

    private bool TryPickPlanetPrisonMap(
        PlanetPrisonStationComponent component,
        bool announceActivation,
        out GameMapPrototype gameMap)
    {
        gameMap = default!;

        var maps = new List<GameMapPrototype>();
        var minPlayers = int.MaxValue;
        var blockedByMinPlayers = false;

        foreach (var station in component.Stations)
        {
            if (!_protoManager.TryIndex(station, out var candidate))
            {
                Log.Warning("No Prison map found, skipping setup.");
                continue;
            }

            if (_playerJoinableMap.CanSpawnGameMap(candidate))
            {
                maps.Add(candidate);
                continue;
            }

            if (!_playerJoinableMap.TryGetGameMapAccessMinPlayers(candidate, out var requiredPlayers))
                continue;

            blockedByMinPlayers = true;
            minPlayers = Math.Min(minPlayers, requiredPlayers);
        }

        if (maps.Count == 0)
        {
            if (blockedByMinPlayers && !announceActivation)
            {
                _chat.DispatchServerAnnouncement(
                    Loc.GetString("planet-prison-not-enough-players", ("minimumPlayers", minPlayers)),
                    Color.OrangeRed);
            }

            return false;
        }

        gameMap = _random.Pick(maps);
        return true;
    }

    private bool AddPlanetPrison(PlanetPrisonStationComponent component, GameMapPrototype gameMap)
    {
        var query = AllEntityQuery<PlanetPrisonStationComponent>();

        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            component.MapId = otherComp.MapId;
            component.PrisonGrid = otherComp.PrisonGrid;
            return true;
        }

        if (!_protoManager.TryIndex(_random.Pick(component.Biomes), out var biome))
        {
            Log.Warning("No Prison map found, skipping setup.");
            return false;
        }

        _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-select-map", ("stationName", gameMap.MapName)), Color.LightBlue);
        _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-select-biome", ("biomeName", biome.ID)), Color.LightBlue);

        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        var uids = _gameTicker.LoadGameMap(gameMap, out var mapId, opts, rot: Angle.Zero);

        component.MapId = mapId;

        if (uids.Count != 1)
        {
            Log.Warning("Prison station have more 1 grid.");
            QueueDel(component.Entity);
            component.Entity = EntityUid.Invalid;

            if (_mapManager.MapExists(component.MapId))
                _mapManager.DeleteMap(component.MapId);

            component.MapId = MapId.Nullspace;
            return false;
        }

        EnsureComp<IgnoreFtlCheckComponent>(uids[0]);
        component.PrisonGrid = uids[0];

        var mapUid = _mapManager.GetMapEntityId(mapId);
        _biomeSystem.EnsurePlanet(mapUid, biome);

        var restricted = new RestrictedRangeComponent
        {
            Origin = new Vector2(0, 0),
            Range = 200,
        };
        AddComp(mapUid, restricted);

        EnsureComp<LightCycleComponent>(mapUid);

        var destComp = _entManager.EnsureComponent<FTLDestinationComponent>(mapUid);
        destComp.BeaconsOnly = true;
        _shuttle.SetFTLWhitelist(mapUid, component.ShuttleWhitelist);
        return true;
    }

}
