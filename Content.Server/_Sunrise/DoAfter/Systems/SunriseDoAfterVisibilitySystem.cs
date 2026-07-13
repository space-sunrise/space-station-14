using Content.Shared._Sunrise.DoAfter.Components;
using Content.Shared._Sunrise.DoAfter.Events;
using Content.Shared.Stealth.Components;

namespace Content.Server._Sunrise.DoAfter.Systems;

public sealed class SunriseDoAfterVisibilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthComponent, ComponentStartup>(OnStealthStartup);
        SubscribeLocalEvent<StealthComponent, ComponentShutdown>(OnStealthShutdown);
        SubscribeLocalEvent<StealthComponent, StealthEnabledChangedEvent>(OnStealthEnabledChanged);
    }

    private void OnStealthStartup(EntityUid uid, StealthComponent component, ComponentStartup args)
    {
        SetDoAfterHidden(uid, component.Enabled);
    }

    private void OnStealthShutdown(EntityUid uid, StealthComponent component, ComponentShutdown args)
    {
        RemComp<SunriseHideDoAfterComponent>(uid);
    }

    private void OnStealthEnabledChanged(EntityUid uid, StealthComponent component, StealthEnabledChangedEvent args)
    {
        SetDoAfterHidden(uid, args.Enabled);
    }

    private void SetDoAfterHidden(EntityUid uid, bool hidden)
    {
        if (hidden)
            EnsureComp<SunriseHideDoAfterComponent>(uid);
        else
            RemComp<SunriseHideDoAfterComponent>(uid);
    }
}
