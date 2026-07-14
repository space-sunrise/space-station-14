using Content.Server.AlertLevel;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed partial class NukeopsRuleSystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NukeopsRuleComponent>();
        while (query.MoveNext(out _, out var nukeops))
        {
            if (!nukeops.CanChangeAlertLevel)
                continue;

            if (_gameTiming.CurTime < nukeops.AlertLevelChangeTime)
                continue;

            if (nukeops.SetAlertlevel == null || nukeops.TargetStation == null)
                continue;

            _alertLevelSystem.SetLevel(nukeops.TargetStation.Value, nukeops.SetAlertlevel, true, true, true, true);
            nukeops.CanChangeAlertLevel = false;
            nukeops.AlertLevelChangeTime = default;
        }
    }

    private void ApplySunriseWarDeclarationAdjustments(NukeopsRuleComponent nukeops)
    {
        nukeops.AlertLevelChangeTime = _gameTiming.CurTime + nukeops.AlertLevelDelay;
        nukeops.CanChangeAlertLevel = true;
    }
}
