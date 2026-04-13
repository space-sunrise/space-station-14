using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Content.Server.Popups;

namespace Content.Server._Sunrise.LockableEquipment;

/// <summary>
/// Handles installing and removing lockable devices from an entity-owned container.
/// </summary>
public sealed class EquipmentContainerSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly LockableEquipmentSystem _lockable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly LayerAccessSystem _layerAccess = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipmentContainerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EquipmentContainerComponent, EquipmentDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EquipmentContainerComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<EquipmentContainerComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
        SubscribeLocalEvent<EquipmentContainerComponent, EquipmentContainerUseHeldKeyVerbEvent>(OnUseHeldKeyVerb);
        SubscribeLocalEvent<EquipmentContainerComponent, EquipmentContainerBreakWithHeldToolVerbEvent>(OnBreakWithHeldToolVerb);
        SubscribeLocalEvent<EquipmentContainerComponent, EquipmentContainerRemoveVerbEvent>(OnRemoveVerb);
    }

    private void OnInteractUsing(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        BaseContainer? container = null;
        EntityUid? installedDevice = null;

        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var existingContainer))
        {
            container = existingContainer;
            installedDevice = FindDevice(existingContainer);
        }

        if (installedDevice != null &&
            TryComp(installedDevice.Value, out LockableEquipmentComponent? installedComp))
        {
            if (!CanAccess(ent.Owner, installedComp.Layer, installedComp))
            {
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-blocked"),
                    args.User,
                    args.User);
                args.Handled = true;
                return;
            }

            if (_lockable.CanRepairWithMaterial(installedDevice.Value, args.Used, installedComp))
            {
                args.Handled = _lockable.TryRepair(installedDevice.Value, args.Used, args.User);
                return;
            }

            if (HasComp<KeyComponent>(args.Used))
            {
                args.Handled = _lockable.TryUseKey(installedDevice.Value, args.Used, args.User);
                return;
            }

            if (_lockable.CanBreakWithTool(installedDevice.Value, args.Used))
            {
                args.Handled = _lockable.TryStartBreakDoAfter(installedDevice.Value, args.Used, args.User, ent.Owner);
                return;
            }
        }

        if (!TryComp(args.Used, out LockableEquipmentComponent? device))
            return;

        container ??= _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
        args.Handled = TryAttachDevice(ent, args.User, args.Used, device, container);
    }

    /// <summary>
    /// Attempts to remove the currently installed device from the target.
    /// </summary>
    public void TryRemove(EntityUid target, EntityUid user)
    {
        if (!TryComp(target, out EquipmentContainerComponent? comp))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(target, comp.ContainerId);
        if (!CanRemove(target, user, container))
            return;

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            comp.DetachDoAfter,
            new EquipmentDoAfterEvent(EquipmentActionType.Detach),
            target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Attempts to start the attach flow for a device held by the user.
    /// </summary>
    public bool TryAttachDevice(Entity<EquipmentContainerComponent> ent, EntityUid user, EntityUid deviceUid, LockableEquipmentComponent device, BaseContainer? container = null)
    {
        container ??= _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);

        if (!CanAttachDevice(ent.Owner, device, container))
        {
            if (!CanAccess(ent.Owner, device.Layer, device))
            {
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-blocked"),
                    user,
                    user);
            }
            else if (FindDevice(container) != null)
            {
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-already"),
                    user,
                    user);
            }

            return false;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.AttachDoAfter,
            new EquipmentDoAfterEvent(EquipmentActionType.Attach),
            ent.Owner,
            target: ent.Owner,
            used: deviceUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<EquipmentContainerComponent> ent, ref EquipmentDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);

        switch (args.Action)
        {
            case EquipmentActionType.Attach:
            {
                if (args.Used is not { } used || !TryComp(used, out LockableEquipmentComponent? device))
                    return;

                if (FindDevice(container) != null)
                    return;

                if (!CanAccess(ent.Owner, device.Layer, device))
                    return;

                if (!_container.Insert(used, container))
                    return;

                var name = MetaData(used).EntityName;
                _popup.PopupClient(
                Loc.GetString("lockable-equipment-equipped", ("name", name)),
                args.User,
                args.User);
                break;
            }

            case EquipmentActionType.Detach:
            {
                var device = FindDevice(container);

                if (device == null)
                {
                    ResetAppearance(ent.Owner);
                    break;
                }

                if (!TryComp(device.Value, out LockableEquipmentComponent? dev))
                    return;

                if (!CanRemove(ent.Owner, args.User, container, quiet: true))
                    return;

                if (!_container.Remove(device.Value, container))
                    return;

                if (!_hands.TryPickup(args.User, device.Value, checkActionBlocker: false))
                    _transform.DropNextTo(device.Value, args.User);

                var name = MetaData(device.Value).EntityName;
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-removed", ("name", name)),
                    args.User,
                    args.User);
                break;
            }
        }

        args.Handled = true;
    }

    //Helpers
    private void UpdateAppearance(EntityUid uid, LockableEquipmentComponent device)
    {
        var appearance = CompOrNull<AppearanceComponent>(uid);
        if (appearance == null)
            return;

        var visualData = CreateVisualData(device, visible: true);

        _appearance.SetData(uid, EquipmentVisuals.VisualData, visualData, appearance);
    }

    private void ResetAppearance(EntityUid uid, LockableEquipmentComponent? previousDevice = null)
    {
        var appearance = CompOrNull<AppearanceComponent>(uid);
        if (appearance == null)
            return;

        EquipmentVisualData? visualData = null;

        if (previousDevice != null)
        {
            visualData = CreateVisualData(previousDevice, visible: false);
        }
        else
        {
            _appearance.TryGetData(uid, EquipmentVisuals.VisualData, out visualData, appearance);
            if (visualData != null)
                visualData = new EquipmentVisualData(false, visualData.Layer, visualData.RsiPath, visualData.State);
        }

        if (visualData != null)
            _appearance.SetData(uid, EquipmentVisuals.VisualData, visualData, appearance);
    }

    private EntityUid? FindDevice(BaseContainer container)
    {
        foreach (var ent in container.ContainedEntities)
        {
            if (HasComp<LockableEquipmentComponent>(ent))
                return ent;
        }

        return null;
    }

    private EntityUid? GetEquipment(EntityUid uid, EquipmentContainerComponent comp)
    {
        if (!_container.TryGetContainer(uid, comp.ContainerId, out var container))
            return null;

        return FindDevice(container);
    }

    private bool CanAccess(EntityUid owner, string layer, LockableEquipmentComponent device)
    {
        return _layerAccess.IsLayerAccessible(owner, layer, device);
    }

    private void OnContainerInserted(Entity<EquipmentContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (TryComp(args.Entity, out LockableEquipmentComponent? device))
            UpdateAppearance(ent.Owner, device);
    }

    private void OnContainerRemoved(Entity<EquipmentContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (TryComp(args.Entity, out LockableEquipmentComponent? device))
            ResetAppearance(ent.Owner, device);
    }

    private static EquipmentVisualData CreateVisualData(LockableEquipmentComponent device, bool visible)
    {
        return new EquipmentVisualData(
            visible,
            device.Layer,
            device.RsiPath,
            device.SpriteState);
    }

    private bool CanAttachDevice(EntityUid owner, LockableEquipmentComponent device, BaseContainer container)
    {
        return CanAccess(owner, device.Layer, device) && FindDevice(container) == null;
    }

    private void OnUseHeldKeyVerb(Entity<EquipmentContainerComponent> ent, ref EquipmentContainerUseHeldKeyVerbEvent args)
    {
        TryUseHeldKey(ent, args.User);
    }

    private void OnBreakWithHeldToolVerb(Entity<EquipmentContainerComponent> ent, ref EquipmentContainerBreakWithHeldToolVerbEvent args)
    {
        TryBreakWithHeldTool(ent, args.User);
    }

    private void OnRemoveVerb(Entity<EquipmentContainerComponent> ent, ref EquipmentContainerRemoveVerbEvent args)
    {
        TryRemove(ent.Owner, args.User);
    }

    private bool TryUseHeldKey(Entity<EquipmentContainerComponent> ent, EntityUid user)
    {
        var device = GetEquipment(ent.Owner, ent.Comp);
        if (device == null)
            return false;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (!_hands.TryGetHeldItem(user, hand, out var held))
                continue;

            if (!HasComp<KeyComponent>(held.Value))
                continue;

            return _lockable.TryUseKey(device.Value, held.Value, user);
        }

        return false;
    }

    private bool TryBreakWithHeldTool(Entity<EquipmentContainerComponent> ent, EntityUid user)
    {
        var device = GetEquipment(ent.Owner, ent.Comp);
        if (device == null || !TryComp(device.Value, out LockableEquipmentComponent? comp))
            return false;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (!_hands.TryGetHeldItem(user, hand, out var held))
                continue;

            if (!_lockable.CanBreakWithTool(device.Value, held.Value, comp))
                continue;

            return _lockable.TryStartBreakDoAfter(device.Value, held.Value, user, ent.Owner);
        }

        return false;
    }

    private bool CanRemove(EntityUid target, EntityUid user, BaseContainer container, bool quiet = false)
    {
        var device = FindDevice(container);
        if (device == null)
            return false;

        if (!TryComp(device.Value, out LockableEquipmentComponent? dev))
            return false;

        if (!CanAccess(target, dev.Layer, dev))
        {
            if (!quiet)
            {
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-blocked"),
                    user,
                    user);
            }

            return false;
        }

        if (dev.Locked)
        {
            if (!quiet)
            {
                var name = MetaData(device.Value).EntityName;
                _popup.PopupClient(
                    Loc.GetString("lockable-equipment-locked", ("name", name)),
                    user,
                    user);
            }

            return false;
        }

        return true;
    }
}
