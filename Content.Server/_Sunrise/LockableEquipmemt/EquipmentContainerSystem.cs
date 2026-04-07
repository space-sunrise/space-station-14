using Content.Shared.Interaction;
using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Verbs;
using Content.Shared.Hands.Components;

using Robust.Server.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Content.Server.Hands.Systems;
using Content.Server.Popups;

namespace Content.Server._Sunrise.LockableEquipment
{
    public sealed class EquipmentContainerSystem : EntitySystem
    {
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly LockableEquipmentSystem _lockable = default!;
        [Dependency] private readonly HandsSystem _hands = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<EquipmentContainerComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<EquipmentContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        }

        private void OnGetVerbs(Entity<EquipmentContainerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            var user = args.User;

            var device = GetEquipment(ent.Owner, ent.Comp);
            if (device == null)
                return;

            if (!TryComp(device.Value, out LockableEquipmentComponent? comp))
                return;

            var name = MetaData(device.Value).EntityName;

            var canAccessLayer = IsLayerAccessible(comp);

            TryComp(user, out HandsComponent? hands);

            if (hands != null)
            {
                foreach (var hand in _hands.EnumerateHands(user))
                {
                    if (!_hands.TryGetHeldItem(user, hand, out var held))
                        continue;

                    if (HasComp<KeyComponent>(held.Value))
                    {
                        var localUser = user;

                        args.Verbs.Add(new InteractionVerb
                        {
                            Text = comp.Locked ? $"Открыть {name}" : $"Закрыть {name}",
                            Priority = 200,
                            Act = () => _lockable.TryUseKey(device.Value, held.Value, localUser)
                        });

                        break;
                    }
                }
            }

            if (canAccessLayer && !comp.Locked)
            {
                var localUser = user;

                args.Verbs.Add(new InteractionVerb
                {
                    Text = $"Снять {name}",
                    Priority = 100,
                    Act = () => TryRemove(ent.Owner, localUser)
                });
            }
        }
        private void OnInteractUsing(Entity<EquipmentContainerComponent> ent, ref InteractUsingEvent args)
        {
            if (!HasComp<LockableEquipmentComponent>(args.Used))
                return;

            if (TryInsert(ent.Owner, args.User, args.Used))
                args.Handled = true;
        }

        private bool TryInsert(EntityUid target, EntityUid user, EntityUid used)
        {
            if (!TryComp(used, out LockableEquipmentComponent? device))
                return false;

            if (GetEquipment(target, Comp<EquipmentContainerComponent>(target)) != null)
            {
                _popup.PopupClient("Уже есть устройство", user, user);
                return true;
            }

            var container = _container.EnsureContainer<Container>(
                target,
                Comp<EquipmentContainerComponent>(target).ContainerId
            );

            if (!_container.Insert(used, container))
                return false;

            var appearance = EnsureComp<AppearanceComponent>(target);

            _appearance.SetData(target, EquipmentVisuals.Visible, true, appearance);
            _appearance.SetData(target, EquipmentVisuals.Sprite, device.rsiPath, appearance);
            _appearance.SetData(target, EquipmentVisuals.Layer, device.Layer, appearance);

            var name = MetaData(used).EntityName;
            _popup.PopupClient($"{name} надето", user, user);

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

            if (TryComp(device.Value, out LockableEquipmentComponent? devComp) && devComp.Locked)
            {
                _popup.PopupClient("Нельзя снять — устройство закрыто", user, user);return;
            }

            var appearance = EnsureComp<AppearanceComponent>(target);

            _container.Remove(device.Value, container);

            _appearance.SetData(target, EquipmentVisuals.Visible, false, appearance);
            _appearance.RemoveData(target, EquipmentVisuals.Sprite, appearance);
            _appearance.RemoveData(target, EquipmentVisuals.Layer, appearance);

            var name = MetaData(device.Value).EntityName;
            _popup.PopupClient($"{name} снято", user, user);
        }

        private void Toggle(EntityUid device, EntityUid user)
        {
            if (!TryComp(device, out LockableEquipmentComponent? comp))
                return;

            comp.Locked = !comp.Locked;

            var name = MetaData(device).EntityName;

            _popup.PopupClient(
                comp.Locked ? $"{name} закрыто" : $"{name} открыто",
                user,
                user
            );
        }

        private bool IsLayerAccessible(LockableEquipmentComponent comp)
        {
            return comp.Layer switch
            {
                "lockable_under" => false,
                _ => true
            };
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
