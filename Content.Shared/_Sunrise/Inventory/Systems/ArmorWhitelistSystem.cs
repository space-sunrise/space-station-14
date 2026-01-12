using Content.Shared.Inventory.Events;
using Content.Shared.Whitelist;
using Content.Shared._Sunrise.Inventory.Components;

namespace Content.Shared._Sunrise.Inventory.Systems;

public sealed class ArmorWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorWhitelistComponent, BeingEquippedAttemptEvent>(OnEquippedAttempt);
    }

    private void OnEquippedAttempt(Entity<ArmorWhitelistComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (_whitelistSystem.CheckBoth(args.EquipTarget, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        args.Cancel();
    }
}
