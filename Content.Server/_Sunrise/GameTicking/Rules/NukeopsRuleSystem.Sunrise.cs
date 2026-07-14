using Content.Server.AlertLevel;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed partial class NukeopsRuleSystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;

    private void ScheduleSunriseWarAlertLevelChange(Entity<NukeopsRuleComponent> nukeops)
    {
        var ruleUid = nukeops.Owner;
        Timer.Spawn(nukeops.Comp.AlertLevelDelay, () => TrySetSunriseWarAlertLevel(ruleUid));
    }

    private void TrySetSunriseWarAlertLevel(EntityUid ruleUid)
    {
        if (!TryComp<NukeopsRuleComponent>(ruleUid, out var nukeops) ||
            nukeops.SetAlertlevel == null ||
            nukeops.TargetStation == null)
            return;

        _alertLevelSystem.SetLevel(nukeops.TargetStation.Value, nukeops.SetAlertlevel, true, true, true, true);
    }
}
