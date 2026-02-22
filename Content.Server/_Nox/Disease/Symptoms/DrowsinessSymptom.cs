// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("DrowsinessSymptom")]
public sealed class DrowsinessSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public static readonly EntProtoId StatusEffectForcedSleeping = "StatusEffectForcedSleeping";

    private const float MinSleepDuration = 5f;
    private const float MaxSleepDuration = 15f;

    public DrowsinessSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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
        var statusEffectsSystem = _entityManager.System<StatusEffectsSystem>();

        var sleepDuration = _random.NextFloat(MinSleepDuration, MaxSleepDuration);
        statusEffectsSystem.TryAddStatusEffectDuration(host, StatusEffectForcedSleeping, TimeSpan.FromSeconds(sleepDuration));
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new DrowsinessSymptom(EffectTimedWindow.Clone());
    }
}
