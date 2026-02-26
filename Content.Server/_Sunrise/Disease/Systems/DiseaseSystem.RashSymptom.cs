// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Server._Sunrise.Disease.Symptoms;
using Robust.Shared.Physics.Events;
using Content.Shared._Sunrise.Disease;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed partial class DiseaseSystem : SharedDiseaseSystem
{
    public void RashInitialize()
    {
        SubscribeLocalEvent<DiseaseComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<DiseaseComponent> ent, ref StartCollideEvent args)
    {
        if (!HasSymptom<RashSymptom>((ent.Owner, ent.Comp)))
        {
            _sawmill.Debug($"[{ent.Owner}] не имеет симптома (RashSymptom)");
            return;
        }

        if (!CanManifestInHost((ent, ent.Comp)))
            return;

        ProbInfect((ent.Owner, ent.Comp), args.OtherEntity);
    }
}