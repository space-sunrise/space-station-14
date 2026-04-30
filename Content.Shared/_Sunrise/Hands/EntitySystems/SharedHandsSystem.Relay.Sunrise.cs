#pragma warning disable IDE0130
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private readonly IConfigurationManager _sunriseConfig = default!;
    [Dependency] private readonly StandingStateSystem _sunriseStandingState = default!;

    private void RelaySunriseMovementSpeedModifiersEvent(Entity<HandsComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!CanSunriseApplyHeldItemSpeedModifiersWhenDowned(entity))
            return;

        CoreRelayEvent(entity, ref args);
    }

    private bool CanSunriseApplyHeldItemSpeedModifiersWhenDowned(Entity<HandsComponent> entity)
    {
        return _sunriseConfig.GetCVar(SunriseCCVars.MovementHeldItemSpeedModifiersWhenDowned)
            || !IsSunriseDowned(entity.Owner);
    }

    private bool IsSunriseDowned(EntityUid uid)
    {
        return HasComp<KnockedDownComponent>(uid) || _sunriseStandingState.IsDown(uid);
    }
}
