using Content.Shared._Sunrise.Abilities.Milira;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;

namespace Content.Client._Sunrise.Abilities.Milira;

/// <summary>
/// Клиентская система WingFlight с блокировкой одевания брони при раскрытых крыльях
/// </summary>
public sealed class WingToggleClientSystem : SharedWingFlightSystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingToggleComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(Entity<WingToggleComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (!ent.Comp.WingsOpened)
            return;

        if (ent.Comp.BlockedSlots != null && ent.Comp.BlockedSlots.Contains(args.Slot))
        {
            if (ent.Comp.AllowedTag != null && _tagSystem.HasTag(args.Equipment, ent.Comp.AllowedTag.Value))
                return;

            args.Cancel();
        }
    }
}

