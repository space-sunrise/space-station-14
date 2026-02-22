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

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);

        var system = _entityManager.System<BlindableSystem>();

        if (_entityManager.TryGetComponent<BlindableComponent>(host, out var component))
        {
            var damage = component.MaxDamage - component.MinDamage;
            _eyeTotalDamage = (int)Math.Round(damage - damage * _eyeDamageProcent);
        }

        system.AdjustEyeDamage((host, component), _eyeTotalDamage);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent virus)
    {
        base.OnRemoved(host, virus);

        var system = _entityManager.System<BlindableSystem>();

        if (_entityManager.TryGetComponent<BlindableComponent>(host, out var component))
            system.AdjustEyeDamage((host, component), -_eyeTotalDamage);
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
        return new BlindableSymptom(EffectTimedWindow.Clone());
    }
}
