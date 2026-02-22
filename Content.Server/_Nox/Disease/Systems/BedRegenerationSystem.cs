// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Nox.Disease.Components;
using Content.Shared.Buckle.Components;
using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease;

namespace Content.Server._Nox.Disease.Systems;

public sealed class BedRegenerationSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BedRegenerationComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<BedRegenerationComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<BedRegenerationComponent> bed, ref StrappedEvent args)
    {
        if (TryComp<DiseaseComponent>(args.Buckle, out var virusComponent))
            virusComponent.RegenerationType = bed.Comp.RegenerationType;
    }

    private void OnUnstrapped(Entity<BedRegenerationComponent> bed, ref UnstrappedEvent args)
    {
        if (TryComp<DiseaseComponent>(args.Buckle, out var virusComponent))
            virusComponent.RegenerationType = BedRegenerationType.None;
    }
}
