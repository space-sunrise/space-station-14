using Content.Server._Sunrise.Mood;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.GhostChillZone;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.Components;
using Content.Shared.Players;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.GhostChillZone;

public sealed class GhostChillZoneSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoaderSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _spawnSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private string _mapPath = string.Empty;
    private bool _enabled;

    private Entity<MapComponent>? _map = null!;
    private bool _loaded;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.GhostChillZoneEnabled, OnValueChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.GhostChillZoneMapPath, newValue => _mapPath = newValue, true);

        _playerManager.PlayerStatusChanged += OnplayerStatusChanged;

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<GhostComponent, GhostChillZoneSpawnRequest>(OnSpawnRequest);
        SubscribeLocalEvent<GhostChillZonePlayerComponent, CanGhostWarpEvent>(OnGhostWarpAttempt);
        SubscribeLocalEvent<GhostChillZonePlayerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<GhostChillZonePlayerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Dead or MobState.Critical)
        {
            QueueDel(ent);
        }
    }

    private void OnGhostWarpAttempt(Entity<GhostChillZonePlayerComponent> ent, ref CanGhostWarpEvent args)
    {
        args.Cancel();
    }

    private void OnSpawnRequest(Entity<GhostComponent> ent, ref GhostChillZoneSpawnRequest args)
    {
        if (!TryComp<ActorComponent>(ent, out var actorComponent))
        {
            return;
        }

        var session = actorComponent.PlayerSession;

        var query = EntityQueryEnumerator<GhostChillZoneSpawnerComponent, TransformComponent>();
        var spawnPoints = new List<TransformComponent>();

        while (query.MoveNext(out var uid, out var _,out var xform))
        {
            spawnPoints.Add(xform);
        }

        if (spawnPoints.Count == 0)
        {
            _chatManager.DispatchServerMessage(session, "", true);
            return;
        }

        var spawnPoint = _random.Pick(spawnPoints);

        var profile = _gameTicker.GetPlayerProfile(session);

        var mob = _spawnSystem.SpawnPlayerMob(spawnPoint.Coordinates, null!, profile, null);

        var mind = session.GetMind();

        if (!mind.HasValue)
        {
            QueueDel(mob);
            return;
        }

        AddComponets(mob);

        _mindSystem.ControlMob(mind.Value, mob);
    }

    private void AddComponets(EntityUid mob)
    {
        EnsureComp<PacifiedComponent>(mob);
        EnsureComp<GhostChillZonePlayerComponent>(mob);

        RemComp<ThirstComponent>(mob);
        RemComp<HungerComponent>(mob);
        RemComp<MoodComponent>(mob);
    }

    private void OnplayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected)
        {
            return;
        }

        QueueDel(e.Session.AttachedEntity);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _loaded = false;

        if (!_map.HasValue)
        {
            return;
        }

        _mapSystem.DeleteMap(_map.Value.Comp.MapId);
        _map = null!;
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        InitMap();
    }

    private void OnValueChanged(bool enabled)
    {
        _enabled = enabled;

        if (_enabled)
        {
            InitMap();
        }
        else if (_map.HasValue)
        {
            _mapSystem.DeleteMap(_map.Value.Comp.MapId);
        }
    }

    private void InitMap()
    {
        if (_loaded || _mapPath == string.Empty)
        {
            return;
        }

        var mapPath = new ResPath(_mapPath);
        var isLoaded = _mapLoaderSystem.TryLoadGrid(mapPath, out _map, out _);

        _loaded = true;
    }

}
