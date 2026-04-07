using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

using Robust.Shared.GameObjects;

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

                _popup.PopupEntity($"{keyName} привязан к {name}", user, user);

                Dirty(keyUid, key);
                Dirty(device, lockComp);
                return true;
            }

            if (key.LockId != lockComp.LockId)
            {
                _popup.PopupEntity($"{keyName} не подходит к {name}", user, user);
                return true;
            }

            lockComp.Locked = !lockComp.Locked;

            _popup.PopupEntity(
                lockComp.Locked ? $"{name} закрыт" : $"{name} открыт",
                user,
                user
            );

            Dirty(device, lockComp);
            return true;
        }

        public bool TryBreak(EntityUid device, EntityUid tool, EntityUid user)
        {
            if (!TryComp<LockableEquipmentComponent>(device, out var comp))
                return false;

            if (!TryComp<TagComponent>(tool, out var tag) ||
                !tag.Tags.Contains(comp.RequiredToolTag))
                return false;

            if (!comp.Locked)
                return false;

            if (IsInUser(device, user))
            {
                _popup.PopupEntity("Вы не можете сделать это на себе", user, user);
                return true;
            }

            var name = MetaData(device).EntityName;

            switch (comp.Mode)
            {
                case LockableEquipmentComponent.BreakMode.None:
                    _popup.PopupEntity($"{name} нельзя взломать", user, user);
                    break;

                case LockableEquipmentComponent.BreakMode.ForceOpen:
                    comp.Locked = false;
                    _popup.PopupEntity($"{name} вскрыт", user, user);
                    break;

                case LockableEquipmentComponent.BreakMode.Breakable:
                    comp.Locked = false;
                    comp.LockId = null;_popup.PopupEntity($"{name} сломан", user, user);
                    break;

                case LockableEquipmentComponent.BreakMode.Destroyable:
                    _popup.PopupEntity($"{name} уничтожен", user, user);
                    QueueDel(device);
                    return true;
            }

            Dirty(device, comp);
            return true;
        }

        private bool IsInUser(EntityUid device, EntityUid user)
        {
            var parent = Transform(device).ParentUid;

            while (parent != EntityUid.Invalid)
            {
                if (parent == user)
                    return true;

                parent = Transform(parent).ParentUid;
            }

            return false;
        }
    }
}
