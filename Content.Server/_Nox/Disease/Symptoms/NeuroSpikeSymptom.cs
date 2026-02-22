// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Jittering;
using Content.Server.Stunnable;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("NeuroSpikeSymptom")]
public sealed class NeuroSpikeSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private TimedWindow _duration = default!;

    public NeuroSpikeSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);

        _duration = new TimedWindow(TimeSpan.FromSeconds(5f), TimeSpan.FromSeconds(10f));
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
        var jitteringSystem = _entityManager.System<SharedJitteringSystem>();
        var stun = _entityManager.System<StunSystem>();
        var timedWindowSystem = _entityManager.System<TimedWindowSystem>();

        timedWindowSystem.Reset(_duration);
        var duration = timedWindowSystem.GetSecondsRemaining(_duration);

        jitteringSystem.DoJitter(host, TimeSpan.FromSeconds(duration), true);
        stun.TryUpdateParalyzeDuration(host, TimeSpan.FromSeconds(duration));
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new NeuroSpikeSymptom(EffectTimedWindow.Clone());
    }
}
