using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Light.Visualizers;

/// <summary>
/// Показывает случайный слой искр на сломанном светильнике.
/// </summary>
[RegisterComponent]
public sealed partial class SunrisePoweredLightSparksComponent : Component
{
    /// <summary>
    /// Карта слоя спрайта, на котором отображаются искры.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Layer = "sunrisePoweredLightSparks";

    /// <summary>
    /// Случайные состояния спрайта, доступные для сломанного светильника.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<string> States = [];

    /// <summary>
    /// Путь к RSI с искрами для fallback-слоя.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ResPath? SparkSprite;

    /// <summary>
    /// Состояние искр, выбранное для конкретного светильника.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string? SelectedState;
}
