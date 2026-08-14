// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Prototypes;
using Content.Shared._Sunrise.Disease.Symptoms;
using Content.Shared._Sunrise.TimeWindow;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Disease;

public interface IDiseaseSymptomFactory
{
    IDiseaseSymptom Create(TimedWindow window);
}


public sealed class DiseaseSymptomFactoryRegistry
{
    private readonly Dictionary<ProtoId<DiseaseSymptomPrototype>, Func<TimedWindow, IDiseaseSymptom>> _factories
        = new();

    public void Register(
        ProtoId<DiseaseSymptomPrototype> id,
        Type type)
    {
        _factories[id] = window =>
        {
            return (IDiseaseSymptom)Activator.CreateInstance(type, window)!;
        };
    }

    public bool Contains(ProtoId<DiseaseSymptomPrototype> id)
    {
        return _factories.ContainsKey(id);
    }

    public IDiseaseSymptom Create(
        ProtoId<DiseaseSymptomPrototype> id,
        TimedWindow window)
    {
        if (!_factories.TryGetValue(id, out var factory))
            throw new Exception($"No factory registered for symptom {id}");

        return factory(window);
    }
}