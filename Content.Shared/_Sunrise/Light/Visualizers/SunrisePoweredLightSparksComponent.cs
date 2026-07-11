using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Light.Visualizers;

/// <summary>
/// Показывает объединенные мерцание и искры на поврежденном светильнике.
/// </summary>
[RegisterComponent]
public sealed partial class SunrisePoweredLightSparksComponent : Component
{
    /// <summary>
    /// Карта слоя спрайта, на котором отображаются искры.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Layer = "sunrisePoweredLightFlicker";

    /// <summary>
    /// Карта слоя спрайта с искрами.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string SparksLayer = "sunrisePoweredLightSparks";

    /// <summary>
    /// Состояния мерцания, доступные для сломанного светильника.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<string> States = [];

    /// <summary>
    /// Состояния искр, проигрываемые одновременно с мерцанием.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<string> SparkStates = [];

    /// <summary>
    /// Путь к RSI с искрами для fallback-слоя.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ResPath? SparkSprite;

    /// <summary>
    /// Детерминированно выбранное состояние мерцания.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string? SelectedState;

    /// <summary>
    /// Детерминированно выбранное состояние искр.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string? SelectedSparkState;
}
