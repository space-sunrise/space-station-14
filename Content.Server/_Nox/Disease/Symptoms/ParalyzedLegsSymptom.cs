// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Movement.Components;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("ParalyzedLegsSymptom")]
public sealed class ParalyzedLegsSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private bool _hasComp = false;

    public ParalyzedLegsSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);

        if (_entityManager.HasComponent<WormComponent>(host))
            _hasComp = true;
        else
            _entityManager.AddComponent<WormComponent>(host);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent virus)
    {
        base.OnRemoved(host, virus);

        if (!_hasComp && _entityManager.HasComponent<WormComponent>(host))
            _entityManager.RemoveComponent<WormComponent>(host);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent virus)
    {
        base.OnUpdate(host, virus);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent virus)
    {

    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new ParalyzedLegsSymptom(EffectTimedWindow.Clone());
    }
}
