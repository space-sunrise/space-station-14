using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Sunrise.Weapons.Melee.Systems;

public sealed class PowerDrainOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<Components.PowerDrainOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, Components.PowerDrainOnMeleeHitComponent comp, ref MeleeHitEvent args)
    {
        if (comp.ChargePerHit <= 0)
            return;

        if (comp.RequireActualHit && (args.HitEntities == null || args.HitEntities.Count == 0))
            return;

        // Prefer slotted power cell if present
        if (HasComp<PowerCellSlotComponent>(uid))
        {
            _powerCell.TryUseCharge(uid, comp.ChargePerHit, null, args.User);
            return;
        }

        // Fall back to direct BatteryComponent on the same entity
        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            _battery.TryUseCharge(uid, comp.ChargePerHit, battery);
        }
    }
}
