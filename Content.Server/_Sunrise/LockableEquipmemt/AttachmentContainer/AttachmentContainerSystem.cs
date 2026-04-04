using Content.Shared.Interaction;
using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Server.Containers;

namespace Content.Server._Sunrise.LockableEquipment.AttachmentContainer
{
    public sealed class AttachmentContainerSystem : EntitySystem
    {
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly LockableEquipmentSystem _lockable = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AttachmentContainerComponent, InteractUsingEvent>(OnUseKeyOnUser);
        }

        private void OnUseKeyOnUser(Entity<AttachmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (!TryComp<KeyComponent>(args.Used, out var key))
                return;

            var belt = GetBelt(ent.Owner, ent.Comp);
            if (belt == null)
                return;

            if (_lockable.TryUseKey(belt.Value, args.Used, args.User))
                args.Handled = true;
        }

        public EntityUid? GetBelt(EntityUid uid, AttachmentContainerComponent comp)
        {
            if (!_container.TryGetContainer(uid, comp.ContainerId, out var container))
                return null;

            foreach (var ent in container.ContainedEntities)
            {
                if (HasComp<LockableEquipmentComponent>(ent))
                    return ent;
            }

            return null;
        }

        public bool TryRemoveBelt(EntityUid user, AttachmentContainerComponent comp)
        {
            var belt = GetBelt(user, comp);
            if (belt == null)
                return false;

            if (TryComp<LockableEquipmentComponent>(belt.Value, out var lockComp)
                && lockComp.Locked)
            {
                _popup.PopupEntity("Устройство заблокировано", user, user);
                return false;
            }

            if (!_container.TryGetContainer(user, comp.ContainerId, out var container))
                return false;

            _container.Remove(belt.Value, container);
            return true;
        }
    }
}
