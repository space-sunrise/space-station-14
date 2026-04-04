using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.LockableEquipment
{
    public sealed class LockableEquipmentSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public bool TryUseKey(EntityUid device, EntityUid keyUid, EntityUid user)
        {
            if (!TryComp<KeyComponent>(keyUid, out var key))
                return false;

            if (!TryComp<LockableEquipmentComponent>(device, out var lockComp))
                return false;

            if (key.LockId == null || lockComp.LockId == null)
                return false;

            if (key.LockId != lockComp.LockId)
            {
                _popup.PopupEntity("Ключ не подходит", user, user);
                return true;
            }

            lockComp.Locked = !lockComp.Locked;

            _popup.PopupEntity(lockComp.Locked ? "Закрыто" : "Открыто", user, user);

            Dirty(device, lockComp);
            return true;
        }

        public bool TryCutWithTool(EntityUid device, EntityUid toolUid, EntityUid user)
        {
            if (!TryComp<LockableEquipmentComponent>(device, out var lockComp))
                return false;

            if (!TryComp<TagComponent>(toolUid, out var tag) ||
                !tag.Tags.Contains("Wirecutter"))
                return false;

            if (!lockComp.Locked)
                return false;

            if (IsHeldBy(device, user))
            {
                _popup.PopupEntity("Нельзя применить на себе", user, user);
                return true;
            }

            lockComp.Locked = false;
            lockComp.LockId = null;

            _popup.PopupEntity("Замок сломан", user, user);

            Dirty(device, lockComp);
            return true;
        }

        private bool IsHeldBy(EntityUid item, EntityUid user)
        {
            return Transform(item).ParentUid == user;
        }
    }
}