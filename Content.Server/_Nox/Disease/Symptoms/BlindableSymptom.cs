// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("BlindableSymptom")]
public sealed class BlindableSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private float _eyeDamageProcent = 0.7f;
    private int _eyeTotalDamage = 0;

    public BlindableSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        var system = _entityManager.System<BlindableSystem>();

        if (!_entityManager.TryGetComponent<BlindableComponent>(host, out var component))
            return;

        var damage = component.MaxDamage - component.MinDamage;
        _eyeTotalDamage = (int)Math.Round(damage - damage * _eyeDamageProcent);

        system.AdjustEyeDamage((host, component), _eyeTotalDamage);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);

        var system = _entityManager.System<BlindableSystem>();

        if (!_entityManager.TryGetComponent<BlindableComponent>(host, out var component))
            return;

        system.AdjustEyeDamage((host, component), -_eyeTotalDamage);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent disease)
    {
        base.OnUpdate(host, disease);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent disease)
    {
        // На случай если слепота/урон глаз был снят чем-то внешним.
        // Важно: доводим EyeDamage до целевого минимума.
        var system = _entityManager.System<BlindableSystem>();

        if (!_entityManager.TryGetComponent<BlindableComponent>(host, out var component))
            return;

        if (_eyeTotalDamage <= 0)
            return;

        var targetDamage = component.MinDamage + _eyeTotalDamage;
        var delta = targetDamage - component.EyeDamage;

        if (delta <= 0)
            return;

        system.AdjustEyeDamage((host, component), delta);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new BlindableSymptom(EffectTimedWindow.Clone());
    }
}
