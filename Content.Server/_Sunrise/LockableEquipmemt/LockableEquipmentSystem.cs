using Content.Shared._Sunrise.LockableEquipment;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server._Sunrise.LockableEquipment
{
public sealed class LockableEquipmentSystem : EntitySystem
    {
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LockableEquipmentComponent, UseKeyOnLockEvent>(OnUseKey);
            SubscribeLocalEvent<LockableEquipmentComponent, GotEquippedEvent>(OnEquipped);
        }
    private void OnUseKey(Entity<LockableEquipmentComponent> ent, ref UseKeyOnLockEvent args)
        {
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
            var comp = ent.Comp;

            if (comp.LockId != null)
                return;

            if (!comp.GenerateKeyOnEquip)
                return;

            comp.LockId = Guid.NewGuid().ToString();

            if (comp.KeyPrototype == null)
                return;

            var coords = Transform(args.Equipee).Coordinates;

            var key = Spawn(comp.KeyPrototype.Value, coords);

            var keyComp = EnsureComp<KeyComponent>(key);
            keyComp.LockId = comp.LockId;

            _hands.TryPickupAnyHand(args.Equipee, key);

            Dirty(ent);
        }
    }
}
