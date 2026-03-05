using Content.Server._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease;
using Content.Shared._Sunrise.Disease.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class SharedDiseaseInfectionDetectorClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public const SlotFlags ValidSlots =
        SlotFlags.HEAD |
        SlotFlags.EYES |
        SlotFlags.MASK;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseInfectionDetectorClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<DiseaseInfectionDetectorClothingComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnGetVisMask(Entity<DiseaseInfectionDetectorUserComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= BaseDiseaseSettings.DiseaseInfectionVisibilityFlag;
    }

    private void OnGotEquipped(Entity<DiseaseInfectionDetectorClothingComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ValidSlots) == 0)
            return;

        var user = EnsureComp<DiseaseInfectionDetectorUserComponent>(args.Equipee);
        user.Count++;

        if (user.Count > 1)
            return;

        _eye.RefreshVisibilityMask(args.Equipee);
    }

    private void OnGotUnequipped(Entity<DiseaseInfectionDetectorClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if ((args.SlotFlags & ValidSlots) == 0)
            return;

        if (!TryComp<DiseaseInfectionDetectorUserComponent>(args.Equipee, out var user))
            return;

        user.Count = Math.Max(0, user.Count - 1);

        if (user.Count > 0)
            return;

        RemComp<DiseaseInfectionDetectorUserComponent>(args.Equipee);
        _eye.RefreshVisibilityMask(args.Equipee);
    }
}