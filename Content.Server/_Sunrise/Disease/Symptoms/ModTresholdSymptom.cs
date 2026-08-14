// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.TimeWindow;
using Content.Shared._Sunrise.Disease.Symptoms;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;

namespace Content.Server._Sunrise.Disease.Symptoms;

[DiseaseSymptom("ModTresholdSymptom")]
public sealed class ModTresholdSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private float _modTresholdModifier = 1.2f; // + 20%
    public ModTresholdSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        var mobThreshold = _entityManager.System<MobThresholdSystem>();

        if (mobThreshold.TryGetThresholdForState(host, MobState.Dead, out var deadThreshold))
            mobThreshold.SetMobStateThreshold(host, deadThreshold.Value * _modTresholdModifier, MobState.Dead);

        if (mobThreshold.TryGetThresholdForState(host, MobState.Critical, out var critThreshold))
            mobThreshold.SetMobStateThreshold(host, critThreshold.Value * _modTresholdModifier, MobState.Critical);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);

        var mobThreshold = _entityManager.System<MobThresholdSystem>();

        if (mobThreshold.TryGetThresholdForState(host, MobState.Dead, out var deadThreshold))
            mobThreshold.SetMobStateThreshold(host, deadThreshold.Value / _modTresholdModifier, MobState.Dead);

        if (mobThreshold.TryGetThresholdForState(host, MobState.Critical, out var critThreshold))
            mobThreshold.SetMobStateThreshold(host, critThreshold.Value / _modTresholdModifier, MobState.Critical);
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
        return new ModTresholdSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }
}
