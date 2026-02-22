// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Symptoms;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Zombies;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("ZombificationSymptom")]
public sealed class ZombificationSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public ZombificationSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        InfectZombieDisease(host);
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
        InfectZombieDisease(host);
    }

    private void InfectZombieDisease(EntityUid target)
    {
        if (_entityManager.HasComponent<ZombieComponent>(target) || _entityManager.HasComponent<ZombieImmuneComponent>(target))
            return;

        // DS14-start

        _entityManager.EnsureComponent<PendingZombieComponent>(target);
        _entityManager.EnsureComponent<ZombifyOnDeathComponent>(target);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new ZombificationSymptom(EffectTimedWindow.Clone());
    }
}
