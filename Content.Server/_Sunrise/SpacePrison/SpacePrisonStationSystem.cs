using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Parallax;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Parallax.Biomes;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.SpacePrison;

public sealed class SpacePrisonStationSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BiomeSystem _biomeSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpacePrisonStationComponent, ComponentInit>(OnSpacePrisonStationInit);
        SubscribeLocalEvent<SpacePrisonStationComponent, ComponentShutdown>(OnCentcommShutdown);
    }
    private void OnCentcommShutdown(EntityUid uid, SpacePrisonStationComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_mapManager.MapExists(component.MapId))
            _mapManager.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
    }

    private void OnSpacePrisonStationInit(EntityUid uid, SpacePrisonStationComponent component, ComponentInit args)
    {
        var minPlayers = _cfg.GetCVar(SunriseCCVars.MinPlayersSpacePrison);
        if (_player.PlayerCount <= minPlayers)
        {
            _chat.DispatchServerAnnouncement($"Недостаточно игроков для Космической Тюрьмы! Необходимо минимум {minPlayers}.", Color.OrangeRed);
            return;
        }
        AddSpacePrison(component);
    }

    private void AddSpacePrison(SpacePrisonStationComponent component)
    {
        var mapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(mapId);
        component.MapId = mapId;
        var station = _random.Pick(component.Stations);

        var mapOptions = new MapLoadOptions()
        {
            LoadMap = false
        };
        var biom = _random.Pick(component.Bioms);
        var MapUid = _mapManager.GetMapEntityId(mapId);

        // Biome
        if (_prototypeManager.TryIndex<GameMapPrototype>(station, out var gameMap))
        {
            _gameTicker.LoadGameMap(gameMap, mapId, mapOptions);
            _map.InitializeMap(mapId);
            _biomeSystem.EnsurePlanet(MapUid, _protoManager.Index<BiomeTemplatePrototype>(biom));
        }
    }
}
