using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.BloodCult.Runes.Systems
{
    public partial class BloodCultSystem
    {
        private void InitializeConstructs()
        {
            SubscribeLocalEvent<ConstructShellComponent, ContainerIsInsertingAttemptEvent>(OnShardInsertAttempt);
            SubscribeLocalEvent<ConstructShellComponent, ComponentInit>(OnShellInit);
            SubscribeLocalEvent<ConstructShellComponent, ComponentRemove>(OnShellRemove);
            SubscribeLocalEvent<ConstructShellComponent, ConstructFormSelectedEvent>(OnShellSelected);
        }

        private void OnShellSelected(EntityUid uid, ConstructShellComponent component, ConstructFormSelectedEvent args)
        {
            var xform = Transform(uid);
            var coordinates = xform.Coordinates;
            if (component.ShardSlot.Item != null)
            {
                var construct = Spawn(args.SelectedForm, coordinates);
                if (!_mindSystem.TryGetMind(component.ShardSlot.Item.Value, out var mindId, out _))
                {
                    _containerSystem.RemoveEntity(uid, component.ShardSlot.Item.Value, force: true);
                    return;
                }

                _mindSystem.TransferTo(mindId, construct);
                QueueDel(uid);
            }
        }

        private void OnShellInit(EntityUid uid, ConstructShellComponent component, ComponentInit args)
        {
            _slotsSystem.AddItemSlot(uid, component.ShardSlotId, component.ShardSlot);
        }

        private void OnShellRemove(EntityUid uid, ConstructShellComponent component, ComponentRemove args)
        {
            _slotsSystem.RemoveItemSlot(uid, component.ShardSlot);
        }

        private void OnShardInsertAttempt(EntityUid uid,
            ConstructShellComponent component,
            ContainerIsInsertingAttemptEvent args)
        {
            if (!_mindSystem.TryGetMind(args.EntityUid, out _, out _) ||
                !TryComp<ActorComponent>(args.EntityUid, out var actor))
            {
                _popupSystem.PopupEntity("Нет души", uid);
                args.Cancel();
                return;
            }

            _slotsSystem.SetLock(uid, component.ShardSlotId, true);
            _ui.OpenUi(uid, SelectConstructUi.Key, actor.PlayerSession);
        }
    }
}
