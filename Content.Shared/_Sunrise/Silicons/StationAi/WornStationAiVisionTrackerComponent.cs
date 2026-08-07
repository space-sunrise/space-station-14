namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Добавляется к сущности, надевшей предмет со <see cref="StationAiVisionComponent"/>.
/// Отслеживает количество надетых предметов, обеспечивающих видимость ИИ.
/// </summary>
[RegisterComponent]
public sealed partial class WornStationAiVisionTrackerComponent : Component
{
    /// <summary>
    /// Количество надетых предметов со <see cref="StationAiVisionComponent"/>.
    /// </summary>
    [DataField]
    public int Count;

    /// <summary>
    /// Был ли <see cref="StationAiVisionComponent"/> добавлен нашей системой
    /// (а не являлся нативным компонентом сущности).
    /// </summary>
    [DataField]
    public bool AddedVisionComponent;
}
