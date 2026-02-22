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

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent virus)
    {
        base.OnRemoved(host, virus);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent virus)
    {
        base.OnUpdate(host, virus);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent virus)
    {
        var virusSystem = _entityManager.System<DiseaseSystem>();
        var vomitSystem = _entityManager.System<VomitSystem>();

        vomitSystem.Vomit(host);
        virusSystem.InfectAround(host);
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
