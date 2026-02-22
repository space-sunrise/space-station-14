// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Damage.Systems;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("NecrosisSymptom")]
public sealed class NecrosisSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private static readonly ProtoId<DamageTypePrototype> NecrosisDamageType = "Cellular";
    private float _minDamage = 1f;
    private float _maxDamage = 10f;

    public NecrosisSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);
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
        var damageableSystem = _entityManager.System<DamageableSystem>();
        var popupSystem = _entityManager.System<PopupSystem>();

        DamageSpecifier dspec = new();
        dspec.DamageDict.Add(NecrosisDamageType, _random.NextFloat(_minDamage, _maxDamage));

        damageableSystem.TryChangeDamage(host,
                            dspec, true);

        var messageKey = _random.Pick(new[]
        {
            "virus-necrosis-popup-1",
            "virus-necrosis-popup-2",
            "virus-necrosis-popup-3",
            "virus-necrosis-popup-4",
            "virus-necrosis-popup-5"
        });

        popupSystem.PopupEntity(Loc.GetString(messageKey), host, host, PopupType.Medium);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new NecrosisSymptom(EffectTimedWindow.Clone());
    }
}
