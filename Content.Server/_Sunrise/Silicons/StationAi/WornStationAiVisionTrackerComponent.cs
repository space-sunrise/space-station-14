namespace Content.Server._Sunrise.Silicons.StationAi;

/// <summary>
/// Добавляется к сущности, надевшей предмет со <see cref="Content.Shared.StationAi.StationAiVisionComponent"/>.
/// Отслеживает количество надетых предметов, дающих видимость ИИ, чтобы корректно
/// удалить компонент при снятии последнего такого предмета.
/// </summary>
[RegisterComponent]
public sealed partial class WornStationAiVisionTrackerComponent : Component
{
    /// <summary>
    /// Количество надетых предметов со <see cref="Content.Shared.StationAi.StationAiVisionComponent"/>.
    /// </summary>
    public int Count;

    /// <summary>
    /// Был ли <see cref="Content.Shared.StationAi.StationAiVisionComponent"/> добавлен нашей системой,
    /// а не являлся нативным компонентом сущности.
    /// </summary>
    public bool AddedVisionComponent;
}
