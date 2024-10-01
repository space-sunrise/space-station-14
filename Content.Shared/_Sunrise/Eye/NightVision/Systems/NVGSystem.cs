using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Sunrise.Eye.NightVision.Systems;

public sealed class PNVSystem : EntitySystem
{
    [Dependency] private readonly NightVisionSystem _nightvisionableSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NVGComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NVGComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<NVGComponent, InventoryRelayedEvent<CanVisionAttemptEvent>>(OnPNVTrySee);
    }

    private void OnPNVTrySee(EntityUid uid, NVGComponent component, InventoryRelayedEvent<CanVisionAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnEquipped(EntityUid uid, NVGComponent component, GotEquippedEvent args)
    {
        if (args.Slot is not ("eyes" or "mask" or "head"))
            return;

        if (HasComp<NightVisionComponent>(args.Equipee))
            return;

        var nvcomp = EnsureComp<NightVisionComponent>(args.Equipee);

        _nightvisionableSystem.UpdateIsNightVision(args.Equipee, nvcomp);
        if (component.ActionContainer == null)
            _actionsSystem.AddAction(args.Equipee, ref component.ActionContainer, component.ActionProto);
        _actionsSystem.SetCooldown(component.ActionContainer, TimeSpan.FromSeconds(1)); // GCD?

        if (nvcomp.PlaySoundOn && nvcomp.IsNightVision)
        {
            if (_net.IsServer)
                _audioSystem.PlayPvs(nvcomp.OnOffSound, uid);
        }

    }

    private void OnUnequipped(EntityUid uid, NVGComponent component, GotUnequippedEvent args)
    {
        if (args.Slot is not ("eyes" or "mask" or "head"))
            return;

        if (!TryComp<NightVisionComponent>(args.Equipee, out var nvcomp))
            return;

        _nightvisionableSystem.UpdateIsNightVision(args.Equipee, nvcomp);
        if (component.ActionContainer != null)
        {
            _actionsSystem.RemoveAction(args.Equipee, component.ActionContainer);
            component.ActionContainer = null;
        }
        
        RemCompDeferred<NightVisionComponent>(args.Equipee);
    }
}