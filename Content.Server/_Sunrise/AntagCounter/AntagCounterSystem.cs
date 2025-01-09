using Content.Server.Roles;
using Content.Shared.Players;
using Prometheus;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.AntagCounter;

public sealed class AntagCounterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly Gauge AntagCountMetric = Metrics
        .CreateGauge("antags_player_count", "Number of antags on the server.");

    private const float CheckDelay = 10;
    private TimeSpan _checkTime;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _checkTime)
            return;

        _checkTime = _timing.CurTime + TimeSpan.FromSeconds(CheckDelay);

        var antagCount = 0;
        foreach (var pSession in Filter.GetAllPlayers())
        {
            if (pSession.Status != SessionStatus.InGame)
                continue;

            var mind = pSession.GetMind();

            if (_role.MindIsAntagonist(mind))
                antagCount += 1;
        }

        AntagCountMetric.Set(antagCount);
    }
}
