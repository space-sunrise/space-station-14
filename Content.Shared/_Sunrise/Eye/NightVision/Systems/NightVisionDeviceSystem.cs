using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared.PowerCell;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Shared.PowerCell.Components;
using Content.Shared.Popups;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using JetBrains.Annotations;

namespace Content.Shared._Sunrise.Eye.NightVision.Systems;

public sealed class NightVisionDeviceSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionDeviceComponent, InventoryRelayedEvent<CanVisionAttemptEvent>>(OnNVDTrySee);
        SubscribeLocalEvent<NightVisionDeviceComponent, NightVisionDeviceUpdateVisualsEvent>(OnNightVisionDeviceUpdateVisuals);

        SubscribeLocalEvent<NightVisionDeviceComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<NightVisionDeviceComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);

        SubscribeLocalEvent<NightVisionDeviceComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<NightVisionDeviceComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<NightVisionDeviceComponent, ComponentShutdown>(OnShutdown);
    }

    private bool HasPowerAndBattery(EntityUid uid)
    {
        if (!TryComp<PowerCellSlotComponent>(uid, out var slot))
            return false;

        if (!_itemSlots.TryGetSlot(uid, slot.CellSlotId, out var itemSlot))
            return false;

        return itemSlot.Item != null && _cell.HasDrawCharge(uid);
    }

    private void OnNVDTrySee(EntityUid uid, NightVisionDeviceComponent component, InventoryRelayedEvent<CanVisionAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnNightVisionDeviceUpdateVisuals(EntityUid uid, NightVisionDeviceComponent component, NightVisionDeviceUpdateVisualsEvent args)
    {
        var updVisEv = new AfterNvdUpdateVisualsEvent();
        RaiseLocalEvent(uid, ref updVisEv);

        _appearance.SetData(uid, NVDVisuals.Light, component.Activated);
    }

    private void OnGetActions(EntityUid uid, NightVisionDeviceComponent component, GetItemActionsEvent args)
    {
        if ((args.SlotFlags & component.RequiredFlags) == component.RequiredFlags)
        {
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
        }
    }

    private void OnShutdown(EntityUid uid, NightVisionDeviceComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ToggleActionEntity);
    }

    private void OnPowerCellSlotEmpty(Entity<NightVisionDeviceComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (ent.Comp.Activated)
            ForceDisable(ent);
    }

    private void OnPowerCellChanged(Entity<NightVisionDeviceComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected || !HasPowerAndBattery(ent.Owner))
        {
            if (ent.Comp.Activated)
                ForceDisable(ent);
        }
    }

    private void OnToggleAction(Entity<NightVisionDeviceComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.IsPowered && !HasPowerAndBattery(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("base-computer-ui-component-not-powered", ("machine", ent.Owner)), args.Performer, args.Performer);
            return;
        }

        Toggle(ent);
        args.Handled = true;
    }

    private void ForceDisable(Entity<NightVisionDeviceComponent> ent)
    {
        ent.Comp.Activated = false;
        var transform = Transform(ent.Owner);

        if (ent.Comp.IsPowered)
        {
            var draw = Comp<PowerCellDrawComponent>(ent.Owner);
            _cell.SetDrawEnabled((ent.Owner, draw), false);
        }

        _appearance.SetData(ent, ToggleableVisuals.Enabled, false);

        var updVisEv = new NightVisionDeviceUpdateVisualsEvent();
        RaiseLocalEvent(ent, ref updVisEv);

        var equipped = transform.ParentUid;
        var changeEv = new NightVisionDeviceToggledEvent(equipped);
        RaiseLocalEvent(ent.Owner, ref changeEv);

        Dirty(ent);
    }

    public void Toggle(Entity<NightVisionDeviceComponent> ent)
    {
        ent.Comp.Activated = !ent.Comp.Activated;
        var transform = Transform(ent.Owner);

        DirtyField(ent.Owner, ent.Comp, nameof(NightVisionDeviceComponent.Activated));

        if (_net.IsServer)
        {
            var sound = ent.Comp.Activated ? ent.Comp.TurnOnSound : ent.Comp.TurnOffSound;
            _audioSystem.PlayPvs(sound, ent.Owner);
        }

        if (ent.Comp.IsPowered)
        {
            var draw = Comp<PowerCellDrawComponent>(ent.Owner);
            _cell.SetDrawEnabled((ent.Owner, draw), ent.Comp.Activated);
        }

        _appearance.SetData(ent, ToggleableVisuals.Enabled, ent.Comp.Activated);

        var updVisEv = new NightVisionDeviceUpdateVisualsEvent();
        RaiseLocalEvent(ent, ref updVisEv);

        var equipped = transform.ParentUid;
        var changeEv = new NightVisionDeviceToggledEvent(equipped);
        RaiseLocalEvent(ent.Owner, ref changeEv);

        Dirty(ent);
    }
}

[ByRefEvent]
public sealed class NightVisionDeviceToggledEvent : EntityEventArgs
{
    public EntityUid Equipped;
    public NightVisionDeviceToggledEvent(EntityUid equipped)
    {
        Equipped = equipped;
    }
};

[PublicAPI, ByRefEvent]
public sealed class AfterNvdUpdateVisualsEvent : EntityEventArgs
{
}
