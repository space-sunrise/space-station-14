// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared._Nox.TimeWindow;
using Robust.Shared.Prototypes;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("AddStaminaSymptom")]
public sealed class AddStaminaSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    private static readonly EntProtoId DiseaseStaminaModifierStatusEffect = "StatusEffectDiseaseStaminaModifier";

    public AddStaminaSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        EnsureStatusEffect(host);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);

        var statusEffectsSystem = _entityManager.System<StatusEffectsSystem>();
        statusEffectsSystem.TryRemoveStatusEffect(host, DiseaseStaminaModifierStatusEffect);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent disease)
    {
        base.OnUpdate(host, disease);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent disease)
    {
        // На случай если статус-эффект был снят чем-то внешним.
        EnsureStatusEffect(host);
    }

    public override IDiseaseSymptom Clone()
    {
        return new AddStaminaSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    private void EnsureStatusEffect(EntityUid host)
    {
        var statusEffectsSystem = _entityManager.System<StatusEffectsSystem>();

        statusEffectsSystem.TrySetStatusEffectDuration(host, DiseaseStaminaModifierStatusEffect, duration: null);
    }
}
