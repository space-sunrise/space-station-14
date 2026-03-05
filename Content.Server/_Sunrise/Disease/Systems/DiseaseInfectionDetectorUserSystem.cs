using Content.Shared._Sunrise.Disease.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class DiseaseInfectionDetectorUserSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentStartup>(OnDetectorUserStartup);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentShutdown>(OnDetectorUserShutdown);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnGetVisMask(Entity<DiseaseInfectionDetectorUserComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= BaseDiseaseSettings.DiseaseInfectionVisibilityFlag;
    }

    private void OnPlayerAttached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref PlayerAttachedEvent args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnPlayerDetached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref PlayerDetachedEvent args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnDetectorUserStartup(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnDetectorUserShutdown(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentShutdown args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }
}