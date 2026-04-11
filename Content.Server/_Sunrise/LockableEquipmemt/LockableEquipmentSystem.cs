using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Interaction;
using Content.Shared.Stacks;

using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.LockableEquipment
{
    public sealed class LockableEquipmentSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedStackSystem _stack = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly LayerAccessSystem _layerAccess = default!;
        [Dependency] private readonly TagSystem _tag = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<LockableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<LockableEquipmentComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<LockableEquipmentComponent, LockableEquipmentBreakDoAfterEvent>(OnBreakDoAfter);
        }

        private void OnStartup(Entity<LockableEquipmentComponent> ent, ref ComponentStartup args)
        {
            UpdateIconState(ent.Owner, ent.Comp);
        }

        private void OnInteractUsing(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled || args.Used == EntityUid.Invalid)
                return;

            if (TryRepair(ent.Owner, args.Used, args.User))
            {
                args.Handled = true;
                return;
            }

            if (TryStartBreakDoAfter(ent.Owner, args.Used, args.User))
            {
                args.Handled = true;
                return;
            }

            if (HasComp<KeyComponent>(args.Used))
            {
                if (TryUseKey(ent.Owner, args.Used, args.User))
                {
                    args.Handled = true;
                    return;
                }
            }
        }

        private void OnBreakDoAfter(Entity<LockableEquipmentComponent> ent, ref LockableEquipmentBreakDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Used is not { } tool)
                return;

            args.Handled = TryBreak(ent.Owner, tool, args.User);
        }

        /// <summary>
        /// Handles key interaction for the device and returns true when the interaction was processed,
        /// including blocked or rejected attempts that already displayed feedback.
        /// </summary>
        public bool TryUseKey(EntityUid device, EntityUid keyUid, EntityUid user)
        {
            if (!TryComp<KeyComponent>(keyUid, out var key))
                return false;

            if (!TryComp<LockableEquipmentComponent>(device, out var lockComp))
                return false;

            if (!EnsureAccessible(device, user, lockComp))
                return true;

            var keyName = MetaData(keyUid).EntityName;
            var name = MetaData(device).EntityName;

            if (lockComp.Broken)
            {
                _popup.PopupEntity(
                    Loc.GetString("lockable-equipment-is-broken", ("name", name)),
                    user,
                    user);
                return true;
            }

            if (key.LockId == null && lockComp.LockId == null)
            {
                var id = Guid.NewGuid().ToString();
                key.LockId = id;
                lockComp.LockId = id;

                _popup.PopupEntity(
                    Loc.GetString("lockable-equipment-paired",
                        ("key", keyName),
                        ("device", name)),
                    user,
                    user);

                Dirty(keyUid, key);
                Dirty(device, lockComp);
                return true;
            }

            if (key.LockId != lockComp.LockId)
            {
                _popup.PopupEntity(
                    Loc.GetString("lockable-equipment-wrong-key",
                        ("key", keyName),
                        ("device", name)),
                    user,
                    user);
                return true;
            }

            lockComp.Locked = !lockComp.Locked;

            var msg = lockComp.Locked
                ? Loc.GetString("lockable-equipment-locked", ("name", name))
                : Loc.GetString("lockable-equipment-unlocked", ("name", name));

            _popup.PopupEntity(msg, user, user);

            UpdateIconState(device, lockComp);
            Dirty(device, lockComp);
            return true;
        }

        /// <summary>
        /// Handles a forced-open attempt and returns true when the interaction was processed,
        /// including blocked attempts that already displayed feedback.
        /// </summary>
        public bool TryStartBreakDoAfter(EntityUid device, EntityUid tool, EntityUid user, EntityUid? interactionTarget = null)
        {
            if (!TryComp<LockableEquipmentComponent>(device, out var comp))
                return false;

            if (!EnsureAccessible(device, user, comp))
                return true;

            if (comp.Mode == LockableEquipmentComponent.BreakMode.None)
            {
                _popup.PopupEntity(
                    Loc.GetString("lockable-equipment-cannot-be-forced-opened", ("name", MetaData(device).EntityName)),
                    user,
                    user);
                return true;
            }

            if (!CanBreakWithTool(device, tool, comp))
                return false;

            if (comp.Broken || !comp.Locked)
                return false;

            var doAfter = new DoAfterArgs(
                EntityManager,
                user,
                comp.BreakDoAfter,
                new LockableEquipmentBreakDoAfterEvent(),
                device,
                target: interactionTarget ?? device,
                used: tool)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent
            };

            return _doAfter.TryStartDoAfter(doAfter);
        }

        /// <summary>
        /// Resolves the configured forced-open behavior and returns true when the interaction was processed,
        /// including blocked attempts that already displayed feedback.
        /// </summary>
        public bool TryBreak(EntityUid device, EntityUid tool, EntityUid user)
        {
            if (!TryComp<LockableEquipmentComponent>(device, out var comp))
                return false;

            if (!EnsureAccessible(device, user, comp))
                return true;

            if (!CanBreakWithTool(device, tool, comp))
                return false;

            if (comp.Broken)
                return false;

            if (!comp.Locked)
                return false;

            if (IsInUser(device, user))
            {
                _popup.PopupEntity(
                    Loc.GetString("lockable-equipment-self-action"),
                    user,
                    user);
                return true;
            }

            var name = MetaData(device).EntityName;

            switch (comp.Mode)
            {
                case LockableEquipmentComponent.BreakMode.None:
                    _popup.PopupEntity(
                        Loc.GetString("lockable-equipment-cannot-be-forced-opened", ("name", name)),
                        user,
                        user);
                    break;

                case LockableEquipmentComponent.BreakMode.ForceOpen:
                    comp.Locked = false;
                    _popup.PopupEntity(
                        Loc.GetString("lockable-equipment-force-open", ("name", name)),
                        user,
                        user);
                    break;

                case LockableEquipmentComponent.BreakMode.Breakable:
                    comp.Locked = false;
                    comp.Broken = true;
                    _popup.PopupEntity(
                        Loc.GetString("lockable-equipment-broken", ("name", name)),
                        user,
                        user);
                    break;

                case LockableEquipmentComponent.BreakMode.Destroyable:
                    _popup.PopupEntity(
                        Loc.GetString("lockable-equipment-destroyed", ("name", name)),
                        user,
                        user);
                    QueueDel(device);
                    return true;
            }

            UpdateIconState(device, comp);
            Dirty(device, comp);
            return true;
        }

        /// <summary>
        /// Attempts to repair a broken device and returns true when the interaction was processed,
        /// including blocked attempts that already displayed feedback.
        /// </summary>
        public bool TryRepair(EntityUid device, EntityUid material, EntityUid user)
        {
            if (!TryComp<LockableEquipmentComponent>(device, out var comp))
                return false;

            if (!EnsureAccessible(device, user, comp))
                return true;

            if (!CanRepairWithMaterial(device, material, comp))
                return false;

            if (!TryComp<StackComponent>(material, out var stack))
                return false;

            if (!_stack.TryUse((material, stack), comp.RepairAmount))
                return false;

            comp.Broken = false;
            comp.Locked = false;

            var name = MetaData(device).EntityName;
            _popup.PopupEntity(
                Loc.GetString("lockable-equipment-repaired", ("name", name)),
                user,
                user);

            UpdateIconState(device, comp);
            Dirty(device, comp);
            return true;
        }

        /// <summary>
        /// Returns true when the given tool can force the device open.
        /// </summary>
        public bool CanBreakWithTool(EntityUid device, EntityUid tool, LockableEquipmentComponent? comp = null)
        {
            if (!Resolve(device, ref comp, false))
                return false;

            return _tag.HasTag(tool, comp.RequiredToolTag);
        }

        /// <summary>
        /// Returns true when the given stack can repair the device.
        /// </summary>
        public bool CanRepairWithMaterial(EntityUid device, EntityUid material, LockableEquipmentComponent? comp = null)
        {
            if (!Resolve(device, ref comp, false))
                return false;

            if (!comp.Broken || comp.RepairMaterial == null || comp.RepairAmount <= 0)
                return false;

            if (!TryComp<StackComponent>(material, out var stack))
                return false;

            return stack.StackTypeId == comp.RepairMaterial && stack.Count >= comp.RepairAmount;
        }

        private bool IsInUser(EntityUid device, EntityUid user)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.TryComp(device, out var deviceXform))
                return false;

            // Walk up the parent chain to see if the user is the root owner
            var current = deviceXform.ParentUid;
            while (current != EntityUid.Invalid)
            {
                if (current.Equals(user))
                    return true;

                if (!xformQuery.TryComp(current, out var xform))
                    break;

                current = xform.ParentUid;
            }

            return false;
        }

        private void UpdateIconState(EntityUid uid, LockableEquipmentComponent comp)
        {
            var appearance = CompOrNull<AppearanceComponent>(uid);
            if (appearance == null)
                return;

            var state = comp.Locked && !comp.Broken ? "icon_locked" : "icon";

            _appearance.SetData(uid, EquipmentVisuals.IconState, state, appearance);
        }

        private bool EnsureAccessible(EntityUid device, EntityUid user, LockableEquipmentComponent comp)
        {
            if (_layerAccess.IsLayerAccessible(ResolveAccessTarget(device), comp.Layer, comp))
                return true;

            _popup.PopupEntity(
                Loc.GetString("lockable-equipment-blocked"),
                user,
                user);
            return false;
        }

        private EntityUid ResolveAccessTarget(EntityUid device)
        {
            if (HasComp<EquipmentContainerComponent>(device))
                return device;

            var current = Transform(device).ParentUid;
            while (current != EntityUid.Invalid)
            {
                if (HasComp<EquipmentContainerComponent>(current))
                    return current;

                if (!TryComp(current, out TransformComponent? xform))
                    break;

                current = xform.ParentUid;
            }

            return device;
        }
    }
}
