// Sunrise-Edit

using Content.Shared._Sunrise.Weapons.Components;
using Content.Shared._Sunrise.Weapons.Enums;
using Content.Shared._Sunrise.Weapons.Events;
using Content.Shared.Inventory;

namespace Content.Server._Sunrise.Weapons.Systems;

public sealed partial class PierceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PierceableComponent, HitScanPierceAttemptEvent>(OnPierceablePierce);
        SubscribeLocalEvent<PierceableComponent, InventoryRelayedEvent<HitScanPierceAttemptEvent>>(OnArmorPierce);
    }

    private void OnArmorPierce(Entity<PierceableComponent> ent, ref InventoryRelayedEvent<HitScanPierceAttemptEvent> args)
    {
        if ((byte)ent.Comp.Level > (byte)args.Args.Level)
            args.Args.Pierced = false;
    }

    private void OnPierceablePierce(Entity<PierceableComponent> ent, ref HitScanPierceAttemptEvent args)
    {
        if ((byte)ent.Comp.Level > (byte)args.Level)
            args.Pierced = false;
    }
}
