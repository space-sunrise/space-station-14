// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared._Nox.Disease.Prototypes;
using Robust.Shared.Prototypes;
using System.Reflection;
using Content.Shared._Nox.Disease;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

public abstract class DiseaseSymptomBase : IDiseaseSymptom
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public TimedWindow EffectTimedWindow { get; }
    public ProtoId<DiseaseSymptomPrototype> PrototypeId { get; }
    protected DiseaseSymptomBase(TimedWindow effectTimedWindow)
    {
        IoCManager.InjectDependencies(this);
        EffectTimedWindow = effectTimedWindow;

        var attr = GetType().GetCustomAttribute<DiseaseSymptomAttribute>();
        if (attr == null)
            throw new Exception($"No DiseaseSymptomAttribute on {GetType()}");

        PrototypeId = attr.Id;
    }

    public virtual void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        ApplyDataEffect(virus.Data, true);
    }

    public virtual void OnRemoved(EntityUid host, DiseaseComponent virus)
    {
        ApplyDataEffect(virus.Data, false);
    }

    public virtual void OnUpdate(EntityUid host, DiseaseComponent virus)
    {
        var timedWindowSystem = _entityManager.System<TimedWindowSystem>();

        if (timedWindowSystem.IsExpired(EffectTimedWindow))
        {
            DoEffect(host, virus);

            if (!BaseDiseaseSettings.DebuffDiseaseMultipliers.TryGetValue(virus.RegenerationType, out var timeMultiplier) || timeMultiplier <= 0f)
                timeMultiplier = 1.0f;

            timedWindowSystem.Reset(
                EffectTimedWindow,
                (float)EffectTimedWindow.Min.TotalSeconds * (1 / timeMultiplier),
                (float)EffectTimedWindow.Max.TotalSeconds * (1 / timeMultiplier)
            );
        }
    }

    public abstract void DoEffect(EntityUid host, DiseaseComponent virus);
    public abstract IDiseaseSymptom Clone();
    public virtual void ApplyDataEffect(DiseaseData data, bool add)
    {
        if (!_prototypeManager.TryIndex(PrototypeId, out var prototype))
            return;

        if (add)
        {
            var timedWindowSystem = _entityManager.System<TimedWindowSystem>();
            timedWindowSystem.Reset(EffectTimedWindow);
            data.Infectivity = Math.Clamp(data.Infectivity + prototype.AddInfectivity, 0, 1);
        }
        else
            data.Infectivity = Math.Clamp(data.Infectivity - prototype.AddInfectivity, 0, 1);
    }
}
