using Content.Server._Sunrise.AssaultOps.Icarus;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ZombieRuleSystem
{
    private const int ZombieIcarusBeamDelay = 25;

    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IcarusTerminalSystem _icarus = default!;

    private void CheckSunriseInfectedThresholds(ZombieRuleComponent zombieRuleComponent, float infectedFraction)
    {
        if (infectedFraction >= zombieRuleComponent.ZombieCburnCallPercentage && !zombieRuleComponent.CburnCalled)
        {
            _gameTicker.StartGameRule(zombieRuleComponent.ZombieCburnGameRule);
            zombieRuleComponent.CburnCalled = true;
        }

        if (infectedFraction >= zombieRuleComponent.ZombieIcarusBeamPercentage && !zombieRuleComponent.IcarusBeamCalled)
        {
            _icarus.FireBeamOnStationDelayed(ZombieIcarusBeamDelay);
            zombieRuleComponent.IcarusBeamCalled = true;
        }
    }
}
