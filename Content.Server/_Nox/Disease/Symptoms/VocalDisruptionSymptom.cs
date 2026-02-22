// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Robust.Shared.Prototypes;
using Content.Server.Speech.Prototypes;
using Content.Server.Speech.Components;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("VocalDisruptionSymptom")]
public sealed class VocalDisruptionSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "virus";
    private ProtoId<ReplacementAccentPrototype>? _oldAccent = null;

    public VocalDisruptionSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);

        if (_entityManager.TryGetComponent<ReplacementAccentComponent>(host, out var component))
            _oldAccent = component.Accent;
        else
        {
            var comp = _entityManager.AddComponent<ReplacementAccentComponent>(host);
            comp.Accent = Accent;
        }
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent virus)
    {
        base.OnRemoved(host, virus);

        if (_entityManager.TryGetComponent<ReplacementAccentComponent>(host, out var component)
            && _oldAccent is { } accent)
            component.Accent = accent;
        else
            _entityManager.RemoveComponent<ReplacementAccentComponent>(host);
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
        return new VocalDisruptionSymptom(EffectTimedWindow.Clone());
    }
}
