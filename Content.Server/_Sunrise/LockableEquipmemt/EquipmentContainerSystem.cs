using Content.Shared.Interaction;
using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Hands.Components;

using Robust.Server.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.LockableEquipment
{
    public sealed class EquipmentContainerSystem : EntitySystem
    {
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<EquipmentContainerComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<EquipmentContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        }

        private void OnGetVerbs(Entity<EquipmentContainerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            var equipment = GetEquipment(ent.Owner, ent.Comp);
            if (equipment == null)
                return;

            TryComp(args.User, out HandsComponent? hands);

            args.Target = equipment.Value;
            RaiseLocalEvent(equipment.Value, ref args);
        }

        private void OnInteractUsing(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            // 👉 надеваем устройство
            if (args.Used != EntityUid.Invalid && HasComp<LockableEquipmentComponent>(args.Used))
            {
                if (TryInsertEquipment(ent, args.User, args.Used))
                    args.Handled = true;

                return;
            }
        }

        private bool TryInsertEquipment(Entity<EquipmentContainerComponent> ent, EntityUid user, EntityUid used)
        {
            if (!HasComp<LockableEquipmentComponent>(used))
                return false;

            if (GetEquipment(ent.Owner, ent.Comp) != null)
            {
                _popup.PopupEntity("Уже есть устройство", user, user);
                return true;
            }

            var container = _container.EnsureContainer<Container>(
                ent.Owner,
                ent.Comp.ContainerId
            );

            if (!_container.Insert(used, container))
                return false;

            _popup.PopupEntity("Устройство надето", user, user);

            ApplyOverlay(ent.Owner, used);

            return true;
        }

        public void TryRemoveEquipment(EntityUid target, EntityUid user)
        {
            if (!TryComp(target, out EquipmentContainerComponent? comp))
                return;

            var container = _container.EnsureContainer<Container>(
                target,
                comp.ContainerId
            );

            var device = FindDevice(container);
            if (device == null)
                return;

            _container.Remove(device.Value, container);

            RemoveOverlay(target);

            _popup.PopupEntity("Устройство снято", user, user);
        }

        // 🔧 overlay добавить
        private void ApplyOverlay(EntityUid target, EntityUid device)
        {
            if (!TryComp(device, out LockableEquipmentComponent? comp))
                return;

            var overlay = EnsureComp<EquipmentOverlayComponent>(target);

            overlay.Layer = comp.OverlayLayer;
            overlay.SpritePath = comp.OverlaySprite;
            overlay.State = "equipped";
            overlay.Visible = true;

            Dirty(target, overlay);
        }

        private void RemoveOverlay(EntityUid target)
        {
            if (!TryComp(target, out EquipmentOverlayComponent? overlay))
                return;

            overlay.Visible = false;
            Dirty(target, overlay);
        }

        private EntityUid? GetEquipment(EntityUid uid, EquipmentContainerComponent comp)
        {
            if (!_container.TryGetContainer(uid, comp.ContainerId, out var container))
                return null;

            return FindDevice(container);
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
    }
}