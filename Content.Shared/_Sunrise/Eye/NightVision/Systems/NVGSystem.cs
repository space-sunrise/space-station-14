using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Log;

namespace Content.Shared._Sunrise.Eye.NightVision.Systems;

public sealed class NVGSystem : EntitySystem
{
    [Dependency] private readonly NightVisionSystem _nightvisionableSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    
    private EntityUid? _equippedNVGItem;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NVGComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NVGComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<NVGComponent, InventoryRelayedEvent<CanVisionAttemptEvent>>(OnPNVTrySee);
        SubscribeLocalEvent<NVGComponent, NVGUpdateVisualsEvent>(OnNVToggled);
    }

    private void OnPNVTrySee(EntityUid uid, NVGComponent component, InventoryRelayedEvent<CanVisionAttemptEvent> args)
    {
        args.Args.Cancel();
    }
    
    private void OnNVToggled(EntityUid uid, NVGComponent component, NVGUpdateVisualsEvent args)
    {
        var nvcomp = args.nvcomp;
        
        _actionsSystem.SetCooldown(component.ActionContainer, TimeSpan.FromSeconds(15));
        

        if (nvcomp.IsNightVision)
        {
            if (_net.IsServer && component is { PlaySounds: true })
                _audioSystem.PlayPvs(component.SoundOn, uid);
            
            if (TryComp<AppearanceComponent>(component.Owner, out var appearance))
                return;
            
            _appearance.SetData(component.Owner, NVGVisuals.Light, NVGContents.Enabled, appearance);
        }
        else if (!nvcomp.IsNightVision)
        {
            if (_net.IsServer && component is { PlaySounds: true })
                _audioSystem.PlayPvs(component.SoundOff, uid);
            
            if (TryComp<AppearanceComponent>(component.Owner, out var appearance))
                return;
            
            _appearance.SetData(component.Owner, NVGVisuals.Light, NVGContents.None, appearance);
        }
        
        if (TryComp<SharedPointLightComponent>(uid, out var light))
            _pointLightSystem.SetEnabled(component.Owner, nvcomp.IsNightVision, light);
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
        _actionsSystem.SetCooldown(component.ActionContainer, TimeSpan.FromSeconds(1)); // GCD?

        if (component.PlaySounds && nvcomp.IsNightVision)
        {
            if (_net.IsServer)
                _audioSystem.PlayPvs(component.SoundOn, uid);
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