using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;

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
    }

    private void OnGetDefault(Entity<HeadsetComponent> ent, ref InventoryRelayedEvent<GetDefaultRadioChannelEvent> args)
    {
        if (!ent.Comp.Enabled || !ent.Comp.IsEquipped)
        {
            // don't provide default channels from pocket slots.
            return;
        }

        if (TryComp(ent, out EncryptionKeyHolderComponent? keyHolder))
            args.Args.Channel ??= keyHolder.DefaultChannel;
    }

    protected virtual void OnGotEquipped(Entity<HeadsetComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.IsEquipped = args.SlotFlags.HasFlag(ent.Comp.RequiredSlot);
        Dirty(ent, ent.Comp);
    }

    protected virtual void OnGotUnequipped(Entity<HeadsetComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.IsEquipped = false;
        Dirty(ent, ent.Comp);
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
