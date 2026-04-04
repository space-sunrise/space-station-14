using Content.Shared._Sunrise.LockableEquipment;
using Robust.Shared.GameObjects;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Log;

namespace Content.Server._Sunrise.LockableEquipment
{
public sealed class LockableEquipmentSystem : EntitySystem
    {
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LockableEquipmentComponent, UseKeyOnLockEvent>(OnUseKey);
            SubscribeLocalEvent<LockableEquipmentComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<LockableEquipmentComponent, BeingUnequippedAttemptEvent>(OnAttemptUnequip);
        }
    private void OnUseKey(Entity<LockableEquipmentComponent> ent, ref UseKeyOnLockEvent args)
        {
            if(args.Handled)
                return;

            if (!TryComp<KeyComponent>(args.Used, out var key))
                return;

            if (key.LockId == null || ent.Comp.LockId == null)
                return;

            if (key.LockId != ent.Comp.LockId)
            {
                _popup.PopupEntity("Ключ не подходит", args.User, args.User);
                return;
            }

            ent.Comp.Locked = !ent.Comp.Locked;

            _popup.PopupEntity(ent.Comp.Locked ? "Закрыто" : "Открыто", args.User, args.User);

            Dirty(ent);

            args.Handled = true;
        }

    private void OnEquipped(Entity<LockableEquipmentComponent> ent, ref GotEquippedEvent args)
        {
            Log.Info("EQUIPPED TRIGGERED");
            var comp = ent.Comp;

            if (comp.LockId != null)
            {
                Log.Info("ccomp.LockId != null");
                return;
            }

            if (!comp.GenerateKeyOnEquip)
            {
                Log.Info("comp.KeyPrototype == null");
                return;
            }

            comp.LockId = Guid.NewGuid().ToString();

            if (comp.KeyPrototype == null)
            {
                Log.Info("comp.KeyPrototype == null");
                return;
            }

            var coords = Transform(args.Equipee).Coordinates;

            var key = Spawn(comp.KeyPrototype.Value, coords);
            Log.Info("spawn");

            var keyComp = EnsureComp<KeyComponent>(key);
            keyComp.LockId = comp.LockId;

            _hands.TryPickupAnyHand(args.Equipee, key);
            Log.Info("TryPickupAnyHand");

            Dirty(ent);
        }
        private void OnAttemptUnequip(Entity<LockableEquipmentComponent> ent, ref BeingUnequippedAttemptEvent args)
        {
            if (!ent.Comp.Locked)
                return;

            args.Cancel();

            if (args.UnEquipTarget != null)
            {
                _popup.PopupEntity("Снять невозможно — замок закрыт", args.UnEquipTarget, args.UnEquipTarget);
            }
        }
    }
}
