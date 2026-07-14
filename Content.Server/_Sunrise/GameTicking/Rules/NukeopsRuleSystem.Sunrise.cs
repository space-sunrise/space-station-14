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
        var query = EntityQueryEnumerator<NukeopsRuleComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (!comp.CanChangeAlertLevel)
                continue;

            if (_gameTiming.CurTime < comp.AlertLevelChangeTime)
                continue;

            if (comp.SetAlertlevel == null || comp.TargetStation == null)
                continue;

            _alertLevelSystem.SetLevel(comp.TargetStation.Value, comp.SetAlertlevel, true, true, true, true);
            comp.CanChangeAlertLevel = false;
        }
    }

    private void ApplySunriseWarDeclarationAdjustments(NukeopsRuleComponent nukeops)
    {
        nukeops.AlertLevelChangeTime = _gameTiming.CurTime + nukeops.AlertLevelDelay;
        nukeops.CanChangeAlertLevel = true;
    }
}
