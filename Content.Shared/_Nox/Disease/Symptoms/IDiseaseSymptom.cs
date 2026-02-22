// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Prototypes;
using Content.Shared._Nox.TimeWindow;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nox.Disease.Symptoms;

public interface IDiseaseSymptom
{
    ProtoId<DiseaseSymptomPrototype> PrototypeId { get; }

    TimedWindow EffectTimedWindow { get; }

    /// <summary>
    ///     Вызывается при добавлении симптома.
    /// </summary>
    void OnAdded(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Периодически вызывается DiseaseSystem, для обновления симптома.
    /// </summary>
    void OnUpdate(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Вызывается при удалении симптома (например, излечение).
    /// </summary>
    void OnRemoved(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Запускает эффект симптома.
    /// </summary>
    void DoEffect(EntityUid host, DiseaseComponent disease);

    /// <summary>
    ///     Метод для передачи симптомов от одного носителя к другому.
    /// </summary>
    IDiseaseSymptom Clone();

    /// <summary>
    ///     Применяет эффект симптома к данным вируса (для SentientDisease).
    /// </summary>
    void ApplyDataEffect(DiseaseData data, bool add);
}
