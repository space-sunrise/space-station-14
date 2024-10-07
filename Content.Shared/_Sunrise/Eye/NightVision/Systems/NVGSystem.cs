using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Log;
using JetBrains.Annotations;

namespace Content.Shared._Sunrise.Eye.NightVision.Systems;

public sealed class NVGSystem : EntitySystem
{
    [Dependency] private readonly NightVisionSystem _nightvisionableSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    
    private EntityUid? _equippedNVGItem;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NVGComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NVGComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<NVGComponent, InventoryRelayedEvent<CanVisionAttemptEvent>>(OnPNVTrySee);
        SubscribeLocalEvent<NVGComponent, NVGUpdateVisualsEvent>(OnNVGUpdateVisuals);
    }

    private void OnPNVTrySee(EntityUid uid, NVGComponent component, InventoryRelayedEvent<CanVisionAttemptEvent> args)
    {
        args.Args.Cancel();
    }
    
    private void OnNVGUpdateVisuals(EntityUid uid, NVGComponent component, NVGUpdateVisualsEvent args)
    {
        var nvcomp = args.nvcomp;
        
        _actionsSystem.SetCooldown(component.ActionContainer, TimeSpan.FromSeconds(5));

        if (!nvcomp.IsNightVision)
        {
            if (_net.IsServer && component.PlaySounds)
                _audioSystem.PlayPvs(component.SoundOn, uid);
        }
        else if (nvcomp.IsNightVision)
        {
            if (_net.IsServer && component.PlaySounds)
                _audioSystem.PlayPvs(component.SoundOff, uid);
        }
        
        var updVisEv = new AfterNVGUpdateVisualsEvent(nvcomp);
        RaiseLocalEvent(component.Owner, ref updVisEv);
    }
    
    public void UpdateVisuals(EntityUid uid, NVGComponent component, bool enable, NightVisionComponent nvcomp)
    {
        if (enable)
        {
            if (_net.IsServer && component.PlaySounds)
                _audioSystem.PlayPvs(component.SoundOn, uid);
        }
        else if (!enable)
        {
            if (_net.IsServer && component.PlaySounds)
                _audioSystem.PlayPvs(component.SoundOff, uid);
        }
        
        var updVisEv = new NVGClientUpdateVisualsEvent(nvcomp, enable);
        RaiseLocalEvent(component.Owner, ref updVisEv);
    }

    private void OnEquipped(EntityUid uid, NVGComponent component, GotEquippedEvent args)
    {
        if (args.Slot is not ("eyes" or "mask" or "head"))
            return;

        if (HasComp<NightVisionComponent>(args.Equipee))
            return;

        var nvcomp = EnsureComp<NightVisionComponent>(args.Equipee);
        
        _equippedNVGItem = args.Equipee;

        _nightvisionableSystem.UpdateIsNightVision(args.Equipee, nvcomp);
        if (component.ActionContainer == null)
            _actionsSystem.AddAction(args.Equipee, ref component.ActionContainer, component.ActionProto);
        _actionsSystem.SetCooldown(component.ActionContainer, TimeSpan.FromSeconds(5)); // GCD?
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
        
        UpdateVisuals(uid ,component, false, nvcomp);
        
        if (_net.IsServer)
            RemCompDeferred<NightVisionComponent>(args.Equipee);
    }
}

[PublicAPI, ByRefEvent]
public sealed class AfterNVGUpdateVisualsEvent : EntityEventArgs {
    public NightVisionComponent nvcomp;
    
    public AfterNVGUpdateVisualsEvent(NightVisionComponent component)
    {
        nvcomp = component;
    }
}

[PublicAPI, ByRefEvent]
public sealed class NVGClientUpdateVisualsEvent : EntityEventArgs {
    public NightVisionComponent nvcomp;
    public bool enable;
    
    public NVGClientUpdateVisualsEvent(NightVisionComponent component, bool isenable)
    {
        nvcomp = component;
        enable = isenable;
        
    }
}