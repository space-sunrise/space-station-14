using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.NPCSleep;

public sealed partial class NPCSleepSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly NPCSystem _npcSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public bool Enabled { get; set; } = true;

    public bool DisableWithoutPlayers { get; set; } = true;

    public float DisableDistance { get; set; } = 20f;

    public TimeSpan NextTick = TimeSpan.Zero;
    public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configurationManager, CCVars.NPCEnabled, value => Enabled = value, true);
        Subs.CVar(_configurationManager, SunriseCCVars.NPCDisableWithoutPlayers, obj => DisableWithoutPlayers = obj, true);
        Subs.CVar(_configurationManager, SunriseCCVars.NPCDisableDistance, obj => DisableDistance = obj, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled || !DisableWithoutPlayers)
            return;

        if (NextTick > _timing.CurTime)
            return;

        NextTick += RefreshCooldown;

        var query = EntityQueryEnumerator<HTNComponent>();

        while(query.MoveNext(out var uid, out var htn))
        {
            if (!_mobStateSystem.IsAlive(uid))
                continue;

            if (HasComp<ActorComponent>(uid))
                continue;

            if (AllowNpc(uid))
            {
                if (!HasComp<ActiveNPCComponent>(uid))
                {
                    _npcSystem.WakeNPC(uid, htn);
                }
            }
            else
            {
                if (HasComp<ActiveNPCComponent>(uid))
                {
                    _npcSystem.SleepNPC(uid, htn);
                }
            }
        }
    }

    private bool AllowNpc(EntityUid uid)
    {
        var xform = Transform(uid);
        var npcCoords = xform.Coordinates.ToMap(EntityManager, _transform);
        var npcMapId = xform.MapID;

        foreach (var playerSession in _playerManager.SessionsDict)
        {
            if (playerSession.Value.AttachedEntity == null)
                continue;

            if (HasComp<GhostComponent>(playerSession.Value.AttachedEntity))
                continue;

            var xformPlayer = Transform(playerSession.Value.AttachedEntity.Value);
            var playerMapId = xformPlayer.MapID;

            if (npcMapId != playerMapId)
                continue;

            var playerCoords = xformPlayer.Coordinates.ToMap(EntityManager, _transform);

            var distance = (npcCoords.Position - playerCoords.Position).Length();

            if (distance < DisableDistance)
                return true;
        }

        return false;
    }
}
