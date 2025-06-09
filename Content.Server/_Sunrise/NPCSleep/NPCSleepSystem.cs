using System.Numerics;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.NPCSleep;

public sealed partial class NPCSleepSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NPCSystem _npcSystem = default!;

    public bool Enabled { get; set; } = true;
    public bool DisableWithoutPlayers { get; set; } = true;
    public float DisableDistance { get; set; } = 20f;
    public float DisableDistanceSquared { get; set; } = 400f; // 20 * 20

    public TimeSpan NextTick = TimeSpan.Zero;
    public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

    private readonly Dictionary<EntityUid, Vector2> _cachedPositions = new();
    private readonly HashSet<EntityUid> _activePlayers = new();
    private readonly HashSet<EntityUid> _activeNPCs = new();
    private readonly HashSet<EntityUid> _deadNPCs = new();
    private readonly Dictionary<MapId, Dictionary<Vector2i, HashSet<EntityUid>>> _spatialHash = new();

    private const float CellSize = 10f;

    private Vector2i GetCell(Vector2 position)
    {
        return new Vector2i(
            (int)(position.X / CellSize),
            (int)(position.Y / CellSize)
        );
    }

    private void UpdateSpatialHash()
    {
        _spatialHash.Clear();

        foreach (var player in _activePlayers)
        {
            if (Deleted(player))
                continue;

            var pos = _cachedPositions[player];
            var mapId = Transform(player).MapID;
            var cell = GetCell(pos);

            if (!_spatialHash.ContainsKey(mapId))
                _spatialHash[mapId] = new Dictionary<Vector2i, HashSet<EntityUid>>();

            if (!_spatialHash[mapId].ContainsKey(cell))
                _spatialHash[mapId][cell] = new HashSet<EntityUid>();

            _spatialHash[mapId][cell].Add(player);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configurationManager, CCVars.NPCEnabled, value => Enabled = value, true);
        Subs.CVar(_configurationManager, SunriseCCVars.NPCDisableWithoutPlayers, obj => DisableWithoutPlayers = obj, true);
        Subs.CVar(_configurationManager, SunriseCCVars.NPCDisableDistance, obj =>
        {
            DisableDistance = obj;
            DisableDistanceSquared = obj * obj;
        }, true);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (HasComp<GhostComponent>(ev.Entity))
            return;

        _activePlayers.Add(ev.Entity);
        _cachedPositions[ev.Entity] = Transform(ev.Entity).WorldPosition;
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        _activePlayers.Remove(ev.Entity);
        _cachedPositions.Remove(ev.Entity);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!HasComp<HTNComponent>(ev.Target))
            return;

        if (ev.NewMobState == MobState.Dead)
        {
            _deadNPCs.Add(ev.Target);
            _activeNPCs.Remove(ev.Target);
        }
        else if (ev.NewMobState == MobState.Alive)
        {
            _deadNPCs.Remove(ev.Target);
            _activeNPCs.Add(ev.Target);
        }
    }

    private bool AllowNpc(EntityUid uid)
    {
        var npcPos = Transform(uid).WorldPosition;
        var npcMapId = Transform(uid).MapID;
        var npcCell = GetCell(npcPos);

        if (!_spatialHash.TryGetValue(npcMapId, out var mapHash))
            return false;

        // Проверяем соседние ячейки
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var cell = new Vector2i(npcCell.X + x, npcCell.Y + y);
                if (!mapHash.TryGetValue(cell, out var players))
                    continue;

                foreach (var player in players)
                {
                    var distanceSquared = (npcPos - _cachedPositions[player]).LengthSquared();
                    if (distanceSquared < DisableDistanceSquared)
                        return true;
                }
            }
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled || !DisableWithoutPlayers)
            return;

        if (NextTick > _timing.CurTime)
            return;

        NextTick += RefreshCooldown;

        foreach (var player in _activePlayers)
        {
            if (Deleted(player))
            {
                _activePlayers.Remove(player);
                _cachedPositions.Remove(player);
                continue;
            }
            _cachedPositions[player] = Transform(player).WorldPosition;
        }

        UpdateSpatialHash();

        var query = EntityQueryEnumerator<HTNComponent>();

        while(query.MoveNext(out var uid, out var htn))
        {
            if (_deadNPCs.Contains(uid))
                continue;

            if (HasComp<ActorComponent>(uid))
                continue;

            if (AllowNpc(uid))
            {
                if (!_activeNPCs.Contains(uid))
                {
                    _npcSystem.WakeNPC(uid, htn);
                    _activeNPCs.Add(uid);
                }
            }
            else
            {
                if (_activeNPCs.Contains(uid))
                {
                    _npcSystem.SleepNPC(uid, htn);
                    _activeNPCs.Remove(uid);
                }
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _cachedPositions.Clear();
        _activePlayers.Clear();
        _activeNPCs.Clear();
        _deadNPCs.Clear();
        _spatialHash.Clear();
    }
}
