// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared._Nox.Disease.Symptoms;
using Content.Shared.Movement.Systems;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("AddSpeedSymptom")]
public sealed class AddSpeedSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private float _addSpeedModifier = 0.2f;
    public AddSpeedSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        var movement = _entityManager.System<MovementSpeedModifierSystem>();

        movement.RefreshMovementSpeedModifiers(host);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);

        var movement = _entityManager.System<MovementSpeedModifierSystem>();

        movement.RefreshMovementSpeedModifiers(host);
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
        return new AddSpeedSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
        if (add)
            data.SpeedModifier += _addSpeedModifier;
        else
            data.SpeedModifier -= _addSpeedModifier;
    }
}
