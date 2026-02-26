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
    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "disease";
    private ProtoId<ReplacementAccentPrototype>? _oldAccent = null;

    public VocalDisruptionSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        var component = _entityManager.EnsureComponent<ReplacementAccentComponent>(host);
        _oldAccent = component.Accent;
        component.Accent = Accent;
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);

        if (_entityManager.TryGetComponent<ReplacementAccentComponent>(host, out var component)
            && _oldAccent is { } accent)
            component.Accent = accent;
        else
            _entityManager.RemoveComponent<ReplacementAccentComponent>(host);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent disease)
    {
        base.OnUpdate(host, disease);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent disease)
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
