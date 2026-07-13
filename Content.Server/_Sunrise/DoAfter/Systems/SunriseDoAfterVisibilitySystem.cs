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

    private void OnStealthStartup(Entity<StealthComponent> ent, ref ComponentStartup args)
    {
        SetDoAfterHidden(ent.Owner, ent.Comp.Enabled);
    }

    private void OnStealthShutdown(Entity<StealthComponent> ent, ref ComponentShutdown args)
    {
        RemComp<SunriseHideDoAfterComponent>(ent.Owner);
    }

    private void OnStealthEnabledChanged(Entity<StealthComponent> ent, ref StealthEnabledChangedEvent args)
    {
        SetDoAfterHidden(ent.Owner, args.Enabled);
    }

    private void SetDoAfterHidden(EntityUid uid, bool hidden)
    {
        if (hidden)
            EnsureComp<SunriseHideDoAfterComponent>(uid);
        else
            RemComp<SunriseHideDoAfterComponent>(uid);
    }
}
