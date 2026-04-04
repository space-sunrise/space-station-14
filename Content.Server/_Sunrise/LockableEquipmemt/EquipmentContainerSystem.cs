using Content.Shared.Interaction;
using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Server.Containers;

namespace Content.Server._Sunrise.LockableEquipment
{
    public sealed class EquipmentContainerSystem : EntitySystem
    {
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly LockableEquipmentSystem _lockable = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<EquipmentContainerComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnInteractUsing(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Used != null)
            {
                if (HasComp<KeyComponent>(args.Used))
                {
                    if (TryHandleKey(ent, ref args))
                        return;
                }
                else if (HasComp<LockableEquipmentComponent>(args.Used))
                {
                    if (TryInsertEquipment(ent, ref args))
                        return;
                }

                return;
            }

            if (TryRemoveEquipment(ent, ref args))
                return;
        }
        private bool TryHandleKey(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (!TryComp<KeyComponent>(args.Used, out var key))
                return false;

            var Equipment = GetEquipment(ent.Owner, ent.Comp);
            if (Equipment == null)
                return false;

            if (_lockable.TryUseKey(Equipment.Value, args.Used, args.User))
            {
                args.Handled = true;
                return true;
            }

            return false;
        }

        private bool TryInsertEquipment(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (HasComp<KeyComponent>(args.Used))
                return false;

            if (!HasComp<LockableEquipmentComponent>(args.Used))
                return false;

            if (GetEquipment(ent.Owner, ent.Comp) != null)
            {
                _popup.PopupEntity("Уже есть устройство", args.User, args.User);
                return true;
            }

            if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
                return false;

            if (!_container.Insert(args.Used, container))
                return false;

            _popup.PopupEntity("Устройство надето", args.User, args.User);

            args.Handled = true;
            return true;
        }

        private bool TryRemoveEquipment(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Used != null)
                return false;

            var Equipment = GetEquipment(ent.Owner, ent.Comp);
            if (Equipment == null)
                return false;

            if (TryComp<LockableEquipmentComponent>(Equipment.Value, out var lockComp)
                && lockComp.Locked)
            {
                _popup.PopupEntity("Устройство заблокировано", args.User, args.User);
                return true;
            }

            if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
                return false;

            _container.Remove(Equipment.Value, container);

            _popup.PopupEntity("Устройство снято", args.User, args.User);

            args.Handled = true;
            return true;
        }

        public EntityUid? GetEquipment(EntityUid uid, EquipmentContainerComponent comp)
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
    }
}
