using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.LockableEquipment
{
    public sealed class LockableEquipmentSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<LockableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<LockableEquipmentComponent, ComponentGetState>(OnGetState);
        }

        private void OnGetState(EntityUid uid, LockableEquipmentComponent component, ref ComponentGetState args)
        {
            args.State = new LockableEquipmentComponentState(
                component.Locked,
                component.LockId,
                component.Layer,
                component.rsiPath,
                component.SpriteState
            );
        }

        private void OnInteractUsing(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Used == EntityUid.Invalid)
                return;

            if (TryBreak(ent.Owner, args.Used, args.User))
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

        public bool TryUseKey(EntityUid device, EntityUid keyUid, EntityUid user)
        {
            if (!TryComp<KeyComponent>(keyUid, out var key))
                return false;

            if (!TryComp<LockableEquipmentComponent>(device, out var lockComp))
                return false;

            var keyName = MetaData(keyUid).EntityName;
            var name = MetaData(device).EntityName;

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


            var msg = lockComp.Locked
                ? Loc.GetString("lockable-equipment-locked", ("name", name))
                : Loc.GetString("lockable-equipment-unlocked", ("name", name));

            _popup.PopupEntity(msg, user, user);

            Dirty(device, lockComp);
            return true;
        }

        public bool TryBreak(EntityUid device, EntityUid tool, EntityUid user)
        {
            if (!TryComp<LockableEquipmentComponent>(device, out var comp))
                return false;

            if (!CanBreakWithTool(device, tool, comp))
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
                    Loc.GetString("lockable-equipment-cannot-be-forced-opened", ("name", name));
                    break;

                case LockableEquipmentComponent.BreakMode.ForceOpen:
                    comp.Locked = false;
                    Loc.GetString("lockable-equipment-force-open", ("name", name));
                    break;

                case LockableEquipmentComponent.BreakMode.Breakable:
                    comp.Locked = false;
                    comp.LockId = null;
                    Loc.GetString("lockable-equipment-broken", ("name", name));
                    break;

                case LockableEquipmentComponent.BreakMode.Destroyable:
                    Loc.GetString("lockable-equipment-destroyed", ("name", name));
                    QueueDel(device);
                    return true;
            }

            Dirty(device, comp);
            return true;
        }

        public bool CanBreakWithTool(EntityUid device, EntityUid tool, LockableEquipmentComponent? comp = null)
        {
            if (!Resolve(device, ref comp, false))
                return false;

            if (comp.Mode == LockableEquipmentComponent.BreakMode.None)
                return false;

            return TryComp<TagComponent>(tool, out var tag) && tag.Tags.Contains(comp.RequiredToolTag);
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
    }
}
