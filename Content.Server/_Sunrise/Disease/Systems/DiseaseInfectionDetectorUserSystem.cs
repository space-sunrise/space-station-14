using Content.Shared._Sunrise.Disease.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class DiseaseInfectionDetectorUserSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentStartup>(OnDetectorUserStartup);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentShutdown>(OnDetectorUserShutdown);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnGetVisMask(Entity<DiseaseInfectionDetectorUserComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= BaseDiseaseSettings.DiseaseInfectionVisibilityFlag;
    }

    private void OnPlayerAttached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _eye.RefreshVisibilityMask(args.Equipee);
    }

    private void OnPlayerDetached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _eye.RefreshVisibilityMask(args.Equipee);
    }

    private void OnDetectorUserStartup(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(args.Equipee);
    }

    private void OnDetectorUserShutdown(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentShutdown args)
    {
        _eye.RefreshVisibilityMask(args.Equipee);
    }
}