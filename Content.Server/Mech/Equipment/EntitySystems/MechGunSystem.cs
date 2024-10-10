using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Mech.Equipment.EntitySystems;
public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>(MechGunShot);
        SubscribeLocalEvent<MechEquipmentComponent, OnEmptyGunShotEvent>(MechEmptyGunShot);
    }

    private void MechGunShot(EntityUid uid, MechEquipmentComponent component, ref GunShotEvent args)
    {
        if (!component.EquipmentOwner.HasValue
            || !HasComp<MechComponent>(component.EquipmentOwner.Value)
            || !TryComp<BatteryComponent>(uid, out var battery))
            return;

        ChargeGunBattery(uid, battery);
    }
    
    private void MechEmptyGunShot(EntityUid uid, MechEquipmentComponent component, ref OnEmptyGunShotEvent args)
    {
        if (!component.EquipmentOwner.HasValue
            || !HasComp<MechComponent>(component.EquipmentOwner.Value)
            || !TryComp<BatteryComponent>(uid, out var battery))
            return;

        ChargeGunBattery(uid, battery);
    }

    private void ChargeGunBattery(EntityUid uid, BatteryComponent component)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var mechEquipment)
            || mechEquipment.EquipmentOwner is null
            || !TryComp<MechComponent>(mechEquipment.EquipmentOwner.Value, out var mech))
            return;

        var chargeDelta = component.MaxCharge - component.CurrentCharge;
        // TODO: The battery charge of the mech would be spent directly when fired.
        if (chargeDelta <= 0
            || mech.Energy - chargeDelta < 0
            || !_mech.TryChangeEnergy(mechEquipment.EquipmentOwner.Value, -chargeDelta, mech))
            return;

        _battery.SetCharge(uid, component.MaxCharge, component);
    }
}