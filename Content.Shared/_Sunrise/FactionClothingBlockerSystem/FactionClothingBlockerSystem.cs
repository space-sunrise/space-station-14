using Content.Shared._Sunrise.Biocode;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Sunrise.FactionClothingBlockerSystem;

public sealed class FactionClothingBlockerSystem : EntitySystem
{
    [Dependency] private readonly BiocodeSystem _biocodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionClothingBlockerComponent, BeingEquippedAttemptEvent>(OnEquippedAttempt);
    }

    private async void OnEquippedAttempt(EntityUid uid, FactionClothingBlockerComponent component, BeingEquippedAttemptEvent args)
    {
        if (TryComp<BiocodeComponent>(args.Equipment, out var biocodedComponent))
        {
            if (!_biocodeSystem.CanUse(args.EquipTarget, biocodedComponent.Factions))
                args.Cancel();
        }
    }
}
