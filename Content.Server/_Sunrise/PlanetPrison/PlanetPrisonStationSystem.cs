using System.Linq;
using System.Numerics;
using Content.Server._Sunrise.NightDayMapLight;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Parallax;
using Content.Server.Shuttles.Systems;
using Content.Shared._Sunrise.AlwaysPoweredMap;
using Content.Shared._Sunrise.Shuttles;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.PlanetPrison;

public sealed class PlanetPrisonStationSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly BiomeSystem _biomeSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("station.prison");
        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentInit>(OnPlanetPrisonStationInit);
        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentShutdown>(OnPrisonShutdown);
    }

    private void OnPrisonShutdown(EntityUid uid, PlanetPrisonStationComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_mapManager.MapExists(component.MapId))
            _mapManager.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
    }

    private void OnPlanetPrisonStationInit(EntityUid uid, PlanetPrisonStationComponent component, ComponentInit args)
    {
        if (TryComp<TransformComponent>(component.Entity, out var xform))
        {
            component.MapId = xform.MapID;
            return;
        }

        var minPlayers = _cfg.GetCVar(SunriseCCVars.MinPlayersPlanetPrison);
        if (_player.PlayerCount <= minPlayers)
        {
            _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-not-enough-players", ("minimumPlayers", minPlayers)), Color.OrangeRed);
            return;
        }
        AddPlanetPrison(component);
    }

    private void AddPlanetPrison(PlanetPrisonStationComponent component)
    {
        var query = AllEntityQuery<PlanetPrisonStationComponent>();

        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            component.MapId = otherComp.MapId;
            return;
        }

        var mapUid = _map.CreateMap();
        var xform = Transform(mapUid);
        component.MapId = xform.MapID;
        var station = _random.Pick(component.Stations);

        var mapOptions = new MapLoadOptions()
        {
            LoadMap = false,
            Rotation = Angle.Zero,
        };

        if (!_protoManager.TryIndex<BiomeTemplatePrototype>(_random.Pick(component.Biomes), out var biome))
        {
            _sawmill.Warning("No Prison map found, skipping setup.");
            return;
        }

        if (!_prototypeManager.TryIndex<GameMapPrototype>(station, out var gameMap))
        {
            _sawmill.Warning("No Prison map found, skipping setup.");
            return;
        }

        _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-select-map", ("stationName", gameMap.MapName)), Color.LightBlue);
       // _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-select-biome", ("biomeName", biome.ID)), Color.LightBlue);

        var uids = _gameTicker.LoadGameMap(gameMap, xform.MapID, mapOptions);

        if (uids.Count != 1)
        {
            _sawmill.Warning("Prison station have more 1 grid.");
            QueueDel(component.Entity);
            component.Entity = EntityUid.Invalid;

            if (_mapManager.MapExists(component.MapId))
                _mapManager.DeleteMap(component.MapId);

            component.MapId = MapId.Nullspace;
            return;
        }

        EnsureComp<IgnoreFtlCheckComponent>(uids[0]);
        component.PrisonGrid = uids[0];

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 21.824779f;
        moles[(int) Gas.Nitrogen] = 82.10312f;

        var mixture = new GasMixture(moles, Atmospherics.T20C);

        _atmos.SetMapAtmosphere(mapUid, false, mixture);

        var gravity = EnsureComp<GravityComponent>(mapUid);
        gravity.Enabled = true;
        gravity.Inherent = true;
        Dirty(mapUid, gravity);

        var light = EnsureComp<MapLightComponent>(mapUid);
        light.AmbientLightColor = Color.FromHex("#D8B059");
        Dirty(mapUid, light);

        // Sunrise-Start
        var restricted = new RestrictedRangeComponent
        {
            Origin = new Vector2(0, 0),
            Range = 160,
        };
        AddComp(mapUid, restricted);
        // Sunrise-End

        EnsureComp<AlwaysPoweredMapComponent>(mapUid);
        EnsureComp<NightDayMapLightComponent>(mapUid);

        EnsureComp<ParallaxComponent>(mapUid, out var parallaxComponent);
        parallaxComponent.Parallax = "Grass";
        Dirty(mapUid, parallaxComponent);

        var destComp = _entManager.EnsureComponent<FTLDestinationComponent>(mapUid);
        destComp.BeaconsOnly = true;
        _shuttle.SetFTLWhitelist(mapUid, component.ShuttleWhitelist);
    }
}
