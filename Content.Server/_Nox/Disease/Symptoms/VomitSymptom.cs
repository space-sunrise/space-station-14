// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Server._Nox.Disease.Systems;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Medical;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("VomitSymptom")]
public sealed class VomitSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public VomitSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent disease)
    {
        base.OnUpdate(host, disease);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent disease)
    {
        var diseaseSystem = _entityManager.System<DiseaseSystem>();
        var vomitSystem = _entityManager.System<VomitSystem>();

        vomitSystem.Vomit(host);
        diseaseSystem.InfectAround(host);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new VomitSymptom(EffectTimedWindow.Clone());
    }
}
