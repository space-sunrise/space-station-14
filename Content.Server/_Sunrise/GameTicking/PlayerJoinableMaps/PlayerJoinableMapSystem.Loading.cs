using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Parallax;
using Content.Server.Shuttles.Systems;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

public sealed partial class PlayerJoinableMapSystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    private readonly Dictionary<ProtoId<PlayerJoinableMapPrototype>, PlayerJoinableMapInstance> _loadedMaps = [];

    private void InitializeManagedLoading()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _loadedMaps.Clear();
    }

    /// <summary>
    /// Attempts to load every managed map that is available during the pre-round lobby.
    /// </summary>
    public void LoadAvailableManagedMaps()
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby || _player.PlayerCount == 0)
            return;

        foreach (var map in _mapIndex.Maps)
        {
            if (map.Load != null)
                TryLoadManagedMap(map.ID);
        }
    }

    /// <summary>
    /// Attempts to load one managed player-joinable map.
    /// </summary>
    /// <returns><see langword="true"/> when the map already exists or was loaded successfully.</returns>
    public bool TryLoadManagedMap(ProtoId<PlayerJoinableMapPrototype> id)
    {
        if (TryGetLoadedMap(id, out _))
            return true;

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby ||
            !_prototype.TryIndex(id, out var map) ||
            map.Load is not { } load)
        {
            return false;
        }

        if (!IsPlayerAccessEnabled(map) && !map.SpawnWhenPlayerAccessDisabled)
            return false;

        if (!TryValidateLoadConfiguration(id, load, out var gameMap, out var biome))
            return false;

        var mapId = MapId.Nullspace;
        try
        {
            var options = DeserializationOptions.Default with { InitializeMaps = false };
            var grids = _gameTicker.LoadGameMap(gameMap, out mapId, options, rot: Angle.Zero);

            if (load.Environment == PlayerJoinableMapEnvironmentType.Planet && grids.Count != 1)
            {
                Log.Error($"Managed planet map {id} must contain exactly one grid, but loaded {grids.Count}.");
                DeleteFailedMap(mapId);
                return false;
            }

            var mapEntity = _map.GetMapOrInvalid(mapId);
            EntityManager.AddComponents(mapEntity, load.MapComponents);
            foreach (var grid in grids)
            {
                EntityManager.AddComponents(grid, load.GridComponents);
            }

            if (biome != null)
                _biome.EnsurePlanet(mapEntity, biome);

            if (load.Ftl is { } ftl)
            {
                if (!_shuttle.TryAddFTLDestination(
                        mapId,
                        ftl.Enabled,
                        ftl.RequireCoordinateDisk,
                        ftl.BeaconsOnly,
                        out _))
                {
                    Log.Error($"Failed to configure FTL destination for managed map {id}.");
                    DeleteFailedMap(mapId);
                    return false;
                }

                _shuttle.SetFTLWhitelist(mapEntity, ftl.ShuttleWhitelist);
            }

            _map.InitializeMap(mapId);
            _loadedMaps.Add(id, new PlayerJoinableMapInstance(mapId, mapEntity, grids));

            if (load.AnnounceOnLoad)
                AnnounceManagedMapLoaded(map, gameMap, biome);

            _stationJobs.UpdateJobsAvailable();
            return true;
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to load managed player-joinable map {id}: {exception}");
            DeleteFailedMap(mapId);
            return false;
        }
    }

    /// <summary>
    /// Gets a live map instance created by the generic loader.
    /// </summary>
    public bool TryGetLoadedMap(
        ProtoId<PlayerJoinableMapPrototype> id,
        out PlayerJoinableMapInstance instance)
    {
        if (_loadedMaps.TryGetValue(id, out instance) && Exists(instance.MapEntity))
            return true;

        _loadedMaps.Remove(id);
        instance = default;
        return false;
    }

    private bool TryValidateLoadConfiguration(
        ProtoId<PlayerJoinableMapPrototype> id,
        PlayerJoinableMapLoadConfiguration load,
        out GameMapPrototype gameMap,
        out BiomeTemplatePrototype? biome)
    {
        gameMap = default!;
        biome = null;

        if (!_prototype.TryIndex(load.GameMap, out GameMapPrototype? resolvedGameMap))
        {
            Log.Error($"Managed player-joinable map {id} references unknown game map {load.GameMap}.");
            return false;
        }

        gameMap = resolvedGameMap;

        switch (load.Environment)
        {
            case PlayerJoinableMapEnvironmentType.Space:
                if (load.Biomes.Count == 0)
                    return true;

                Log.Error($"Managed space map {id} must not declare planet biomes.");
                return false;
            case PlayerJoinableMapEnvironmentType.Planet:
                if (load.Biomes.Count == 0)
                {
                    Log.Error($"Managed planet map {id} must declare at least one biome.");
                    return false;
                }

                var biomeId = _random.Pick(load.Biomes);
                if (_prototype.TryIndex(biomeId, out biome))
                    return true;

                Log.Error($"Managed planet map {id} references unknown biome {biomeId}.");
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(load.Environment), load.Environment, null);
        }
    }

    private void DeleteFailedMap(MapId mapId)
    {
        if (mapId != MapId.Nullspace && _map.MapExists(mapId))
            _map.DeleteMap(mapId);
    }

    private void AnnounceManagedMapLoaded(
        PlayerJoinableMapPrototype map,
        GameMapPrototype gameMap,
        BiomeTemplatePrototype? biome)
    {
        _chat.DispatchServerAnnouncement(
            Loc.GetString("player-joinable-map-selected-map", ("map", gameMap.MapName)),
            Color.LightBlue);

        if (biome != null)
        {
            _chat.DispatchServerAnnouncement(
                Loc.GetString("player-joinable-map-selected-biome", ("biome", biome.ID)),
                Color.LightBlue);
        }

        if (!IsPlayerAccessEnabled(map) ||
            !PlayerJoinableMapAccess.TryGetMinPlayers(map, _cfg, out var minPlayers) ||
            Math.Max(0, minPlayers) == 0 ||
            !_playerCountAccessibleMaps.Contains(map.ID))
        {
            return;
        }

        _chat.DispatchServerAnnouncement(
            Loc.GetString("player-joinable-map-module-activated", ("module", Loc.GetString(map.DisplayName))),
            Color.LightBlue);
    }
}
