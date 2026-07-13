using Content.Shared._Sunrise.DoAfter.Components;
using Content.Shared.Stealth.Components;

namespace Content.Server._Sunrise.DoAfter.Systems;

public sealed class SunriseDoAfterVisibilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthComponent, ComponentStartup>(OnStealthStartup);
        SubscribeLocalEvent<StealthComponent, ComponentShutdown>(OnStealthShutdown);
    }

    private void OnStealthStartup(EntityUid uid, StealthComponent component, ComponentStartup args)
    {
        EnsureComp<SunriseHideDoAfterComponent>(uid);
    }

    private void OnStealthShutdown(EntityUid uid, StealthComponent component, ComponentShutdown args)
    {
        RemComp<SunriseHideDoAfterComponent>(uid);
    }
}
