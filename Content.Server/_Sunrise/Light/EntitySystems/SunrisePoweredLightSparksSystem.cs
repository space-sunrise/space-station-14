using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Sunrise.Light.Visualizers;
using Content.Shared.Power;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Light.EntitySystems;

public sealed class SunrisePoweredLightSparksSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.25);

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SunrisePoweredLightSparksComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SunrisePoweredLightSparksComponent, ExtensionCableSystem.ProviderConnectedEvent>(OnProviderConnected);
        SubscribeLocalEvent<SunrisePoweredLightSparksComponent, ExtensionCableSystem.ProviderDisconnectedEvent>(OnProviderDisconnected);
        SubscribeLocalEvent<SunrisePoweredLightSparksComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnStartup(Entity<SunrisePoweredLightSparksComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnProviderConnected(Entity<SunrisePoweredLightSparksComponent> ent, ref ExtensionCableSystem.ProviderConnectedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnProviderDisconnected(Entity<SunrisePoweredLightSparksComponent> ent, ref ExtensionCableSystem.ProviderDisconnectedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnPowerChanged(Entity<SunrisePoweredLightSparksComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    public override void Update(float frameTime)
    {
        if (_nextUpdate > _timing.CurTime)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<SunrisePoweredLightSparksComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var sparks, out var powerReceiver))
        {
            UpdateAppearance((uid, sparks), powerReceiver);
        }
    }

    private void UpdateAppearance(
        Entity<SunrisePoweredLightSparksComponent> ent,
        ApcPowerReceiverComponent? powerReceiver = null)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance) ||
            !Resolve(ent, ref powerReceiver, false))
        {
            return;
        }

        var hasPower = powerReceiver.Powered && powerReceiver.NetworkLoad.LinkedNetwork != default;
        _appearance.SetData(ent, SunrisePoweredLightVisuals.HasPower, hasPower, appearance);
    }
}
