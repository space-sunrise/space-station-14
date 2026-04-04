using Content.Shared._Sunrise.LockableEquipment;
using Robust.Shared.GameObjects;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Robust.Shared.Log;

namespace Content.Server._Sunrise.LockableEquipment
{
public sealed class LockableEquipmentSystem : EntitySystem
    {
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LockableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<LockableEquipmentComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<LockableEquipmentComponent, BeingUnequippedAttemptEvent>(OnAttemptUnequip);
        }

        private void OnInteractUsing(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (TryCutWithTool(ent, ref args))
                return;

            if (TryUseKey(ent, ref args))
                return;
        }

        private void OnEquipped(Entity<LockableEquipmentComponent> ent, ref GotEquippedEvent args)
        {
            if (!ShouldGenerateKey(ent))
                return;

            GenerateKey(ent, args.Equipee);
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

        private bool TryCutWithTool(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
        {
            if (!TryComp<TagComponent>(args.Used, out var tag) ||
                !tag.Tags.Contains("Wirecutter"))
                return false;

            if (!ent.Comp.Locked)
                return false;

            var parent = Transform(ent).ParentUid;

            if (_inventory.TryGetContainingSlot(ent.Owner, out var slot) && slot.Owner == args.User)
            {
                _popup.PopupEntity("Вы не можете сломать замок на себе", args.User, args.User);
                return true;
            }

            ent.Comp.Locked = false;
            ent.Comp.LockId = null;

            _popup.PopupEntity("Замок сломан", args.User, args.User);

            Dirty(ent);

            args.Handled = true;
            return true;
        }

        private bool TryUseKey(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
        {
            if (!TryComp<KeyComponent>(args.Used, out var key))
                return false;

            if (key.LockId == null || ent.Comp.LockId == null)
                return false;

            if (key.LockId != ent.Comp.LockId)
            {
                _popup.PopupEntity("Ключ не подходит", args.User, args.User);
                return true;
            }

            ent.Comp.Locked = !ent.Comp.Locked;

            _popup.PopupEntity(ent.Comp.Locked ? "Закрыто" : "Открыто", args.User, args.User);

            Dirty(ent);

            args.Handled = true;
            return true;
        }

        private bool ShouldGenerateKey(Entity<LockableEquipmentComponent> ent)
        {
            var comp = ent.Comp;

            if (comp.LockId != null)
                return false;

            if (!comp.GenerateKeyOnEquip)
                return false;

            if (comp.KeyPrototype == null)
                return false;

            return true;
        }

        private void GenerateKey(Entity<LockableEquipmentComponent> ent, EntityUid equipee)
        {
            var comp = ent.Comp;

            comp.LockId = Guid.NewGuid().ToString();

            var coords = Transform(equipee).Coordinates;

            var prototype = comp.KeyPrototype;
            if (prototype == null)
                return;

            var key = Spawn(prototype.Value, coords);

            var keyComp = EnsureComp<KeyComponent>(key);
            keyComp.LockId = comp.LockId;

            if (!_hands.TryPickupAnyHand(equipee, key))
            {
                Log.Info("Руки заняты");
            }

            Dirty(ent);
        }
    }
}
