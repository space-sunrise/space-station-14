using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.NPC;

public sealed partial class NpcSleepSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public bool Enabled = true;
    public bool DisableWithoutPlayers = true;

    public float DisableDistance = 20f;

    private TimeSpan _nextCheckTime = TimeSpan.Zero;
    private static readonly TimeSpan CheckCooldown = TimeSpan.FromSeconds(5);

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<ActiveNPCComponent> _activeQuery;
    private EntityQuery<GhostComponent> _ghostQuery;

    private readonly HashSet<Entity<ActorComponent>> _players = [];

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, CCVars.NPCEnabled, value => Enabled = value, true);
        Subs.CVar(_configuration, SunriseCCVars.NpcDisableWithoutPlayers, obj => DisableWithoutPlayers = obj, true);
        Subs.CVar(_configuration, SunriseCCVars.NpcDisableDistance, obj => DisableDistance = obj, true);

        _actorQuery = GetEntityQuery<ActorComponent>();
        _activeQuery = GetEntityQuery<ActiveNPCComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled || !DisableWithoutPlayers)
            return;

        if (_timing.CurTime < _nextCheckTime)
            return;

        _nextCheckTime = _timing.CurTime + CheckCooldown;

        var query = EntityQueryEnumerator<HTNComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var htn, out var xform))
        {
            if (!_mobState.IsAlive(uid))
                continue;

            if (_actorQuery.HasComp(uid))
                continue;

            var isActive = _activeQuery.HasComponent(uid);
            if (AllowNpc(uid, xform))
            {
                if (!isActive)
                    _npc.WakeNPC(uid, htn);
            }
            else
            {
                if (isActive)
                    _npc.SleepNPC(uid, htn);
            }
        }
    }

    private bool AllowNpc(EntityUid uid, TransformComponent xform)
    {
        var npcCoords = _transform.GetMapCoordinates(uid, xform);

        _players.Clear();
        _lookup.GetEntitiesInRange(npcCoords,
            DisableDistance,
            _players,
            LookupFlags.Dynamic | LookupFlags.Approximate);

        foreach (var ent in _players)
        {
            if (_ghostQuery.HasComp(ent))
                continue;

            // Достаточно одного подходящего игрока
            return true;
        }

        return false;
    }
}
