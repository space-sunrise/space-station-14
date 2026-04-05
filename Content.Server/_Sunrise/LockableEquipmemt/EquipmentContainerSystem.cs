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
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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

            var ev = new GetVerbsEvent<InteractionVerb>(
                args.User,
                equipment.Value,
                args.Using,
                hands,
                args.CanInteract,
                args.CanInteract,
                args.CanAccess,
                new List<VerbCategory>()
            );

            RaiseLocalEvent(equipment.Value, ev);
            args.Verbs.UnionWith(ev.Verbs);
        }

        private void OnInteractUsing(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (!HasComp<LockableEquipmentComponent>(args.Used))
                return;

            if (TryInsert(ent, args.User, args.Used))
                args.Handled = true;
        }

        private bool TryInsert(Entity<EquipmentContainerComponent> ent, EntityUid user, EntityUid used)
        {
            if (!TryComp(used, out LockableEquipmentComponent? device))
                return false;

            if (GetEquipment(ent.Owner, ent.Comp) != null)
            {
                var name = MetaData(used).EntityName;
                _popup.PopupClient($"Уже есть {name}", user, user);
                return true;
            }

            var container = _container.EnsureContainer<Container>(
                ent.Owner,
                ent.Comp.ContainerId
            );

            if (!_container.Insert(used, container))
                return false;

            ApplyAppearance(ent.Owner, device);

            var deviceName = MetaData(used).EntityName;
            _popup.PopupClient($"{deviceName} надето", user, user);

            return true;
        }

        public void TryRemove(EntityUid target, EntityUid user)
        {
            if (!TryComp(target, out EquipmentContainerComponent? comp))
                return;

            var container = _container.EnsureContainer<Container>(target, comp.ContainerId);
            var device = FindDevice(container);

            if (device == null)
                return;

            _container.Remove(device.Value, container);

            RemoveAppearance(target);

            var name = MetaData(device.Value).EntityName;
            _popup.PopupClient($"{name} снято", user, user);
        }

        private void ApplyAppearance(EntityUid target, LockableEquipmentComponent device)
        {
            var appearance = EnsureComp<AppearanceComponent>(target);

            _appearance.SetData(target, EquipmentVisuals.Visible, true, appearance);
            _appearance.SetData(target, EquipmentVisuals.Layer, device.OverlayLayer, appearance);
        }

        private void RemoveAppearance(EntityUid target)
        {
            if (!TryComp(target, out AppearanceComponent? appearance))
                return;

            _appearance.SetData(target, EquipmentVisuals.Visible, false, appearance);
            _appearance.RemoveData(target, EquipmentVisuals.Layer, appearance);
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
