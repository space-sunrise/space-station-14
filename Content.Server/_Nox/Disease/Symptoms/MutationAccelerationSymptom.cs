// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("MutationAccelerationSymptom")]
public sealed class MutationAccelerationSymptom : DiseaseSymptomBase
{
    private int _addRegenMutationPoints = 4;

    public MutationAccelerationSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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

    }

    public override IDiseaseSymptom Clone()
    {
        return new MutationAccelerationSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
        if (add)
            data.RegenMutationPoints += _addRegenMutationPoints;
        else
            data.RegenMutationPoints -= _addRegenMutationPoints;
    }
}
