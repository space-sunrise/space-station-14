using Content.Server.Disposal.Unit;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Server._Sunrise.Misc.BlindInDisposals;

/// <summary>
/// Простая система, делающая персонажа слепым, пока он находится в трубах.
/// Из труб не должно быть видно реальный мир!!
/// </summary>
public sealed class BlindInDisposalsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<BeingDisposedComponent> ent, ref ComponentStartup args)
    {
        if (!HasComp<BlindableComponent>(ent))
            return;

        EnsureComp<TemporaryBlindnessComponent>(ent);
    }

    private void OnShutdown(Entity<BeingDisposedComponent> ent, ref ComponentShutdown args)
    {
        if (!HasComp<BlindableComponent>(ent))
            return;

        RemComp<TemporaryBlindnessComponent>(ent);
    }
}
