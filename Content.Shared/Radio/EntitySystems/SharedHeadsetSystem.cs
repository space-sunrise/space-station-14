using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;
using Content.Shared._Sunrise.Radio;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedHeadsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadsetComponent, InventoryRelayedEvent<GetDefaultRadioChannelEvent>>(OnGetDefault);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);

        // Sunrise-Start
        SubscribeLocalEvent<HeadsetComponent, HeadsetToggleChannelMessage>(OnToggleChannel);
        SubscribeLocalEvent<HeadsetComponent, HeadsetChangeVolumeMessage>(OnChangeVolume);
        // Sunrise-End
    }

    // Sunrise-Start
    private void OnToggleChannel(EntityUid uid, HeadsetComponent component, HeadsetToggleChannelMessage args)
    {
        component.EnabledChannels[args.ChannelId] = args.Enabled;
        Dirty(uid, component);
    }

    private void OnChangeVolume(EntityUid uid, HeadsetComponent component, HeadsetChangeVolumeMessage args)
    {
        component.ChannelVolumes[args.ChannelId] = args.Volume;
        Dirty(uid, component);
    }
    // Sunrise-End

    private void OnGetDefault(EntityUid uid, HeadsetComponent component, InventoryRelayedEvent<GetDefaultRadioChannelEvent> args)
    {
        if (!component.Enabled || !component.IsEquipped)
        {
            // don't provide default channels from pocket slots.
            return;
        }

        if (TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            args.Args.Channel ??= keyHolder.DefaultChannel;
    }

    protected virtual void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);
        Dirty(uid, component);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
        Dirty(uid, component);
    }

    private void OnEmpPulse(Entity<HeadsetComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Enabled)
        {
            args.Affected = true;
            args.Disabled = true;
        }
    }
}
