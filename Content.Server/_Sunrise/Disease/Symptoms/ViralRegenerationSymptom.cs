// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.TimeWindow;
using Content.Shared._Sunrise.Disease.Symptoms;

namespace Content.Server._Sunrise.Disease.Symptoms;

[DiseaseSymptom("ViralRegenerationSymptom")]
public sealed class MedViralRegenerationSymptom : DiseaseSymptomBase
{
    private float _addRegenThreshold = 0.5f;

    public MedViralRegenerationSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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

    }

    public override IDiseaseSymptom Clone()
    {
        return new MedViralRegenerationSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
        if (add)
            data.RegenThreshold += _addRegenThreshold;
        else
            data.RegenThreshold -= _addRegenThreshold;
    }
}
