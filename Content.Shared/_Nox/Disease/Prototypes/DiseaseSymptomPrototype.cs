// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.TimeWindow;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nox.Disease.Prototypes;

[Prototype]
public sealed partial class DiseaseSymptomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public string Description { get; private set; } = default!;

    /// <summary>
    ///     Количество прибавляемой заразности симптому в процентах.
    /// </summary>
    [DataField]
    public float AddInfectivity { get; private set; } = 0.02f;

    /// <summary>
    ///     Цена мутации.
    /// </summary>
    [DataField]
    public int Price = 100;

    /// <summary>
    ///     Индикатор, требуется для управления сиптомами в случайных вирусах событий игры.
    /// </summary>
    [DataField("danger", required: true)]
    public DangerIndicatorSymptom DangerIndicator;

    /// <summary>
    ///     Интервал срабатывания симптома.
    /// </summary>
    [DataField]
    public TimedWindow IntervalWindow = new TimedWindow(TimeSpan.FromSeconds(15f), TimeSpan.FromSeconds(60f));
}

public enum DangerIndicatorSymptom
{
    Low = 0,
    Medium,
    High,
    Cataclysm
}

