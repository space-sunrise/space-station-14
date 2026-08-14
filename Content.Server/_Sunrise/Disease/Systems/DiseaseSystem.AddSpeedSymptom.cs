// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease;
using Content.Shared.Movement.Systems;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed partial class DiseaseSystem : SharedDiseaseSystem
{
    public void AddSpeedInitialize()
    {
        SubscribeLocalEvent<DiseaseComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<DiseaseComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.Data.SpeedModifier, ent.Comp.Data.SpeedModifier);
    }
}