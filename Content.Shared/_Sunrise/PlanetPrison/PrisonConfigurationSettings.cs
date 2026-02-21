using Robust.Shared.Map;

namespace Content.Shared._Sunrise.PlanetPrison;

/// <summary>
/// Режим завершения карты тюрьмы
/// </summary>
public enum PrisonMapCompletionMode
{
    /// <summary>
    /// Заморозить карту после завершения и удалить при рестарте раунда.
    /// </summary>
    Freeze,

    /// <summary>
    /// Сразу удалить карту после завершения (все игроки мертвы).
    /// </summary>
    Delete
}

/// <summary>
/// Настройки завершения карт тюрьмы
/// </summary>
[DataRecord]
public sealed partial record PrisonMapCompletionSettings
{
    [DataField] public PrisonMapCompletionMode CompletionMode { get; init; } = PrisonMapCompletionMode.Delete;
}

/// <summary>
/// Диапазон MapId для одного семейства карт тюрьмы (например Nox: 500..550).
/// </summary>
[DataRecord]
public sealed partial record PrisonFamilyMapIdRange
{
    [DataField] public int Start { get; init; }
    [DataField] public int Size { get; init; }
}

/// <summary>
/// Настройки кэширования карт тюрьмы. Используется только дефолт; размер кэша для каждой карты задаётся в прототипе карты (PrisonCacheSize).
/// </summary>
[DataRecord]
public sealed partial record PrisonCacheSettings
{
    [DataField] public int DefaultPrisonCacheSize { get; init; } = 1;

    /// <summary>
    /// Диапазоны MapId по имени семейства (ключ — PrisonMapFamily). Если не задано, при отсутствии prisonCacheMapIdStart используется глобальный поиск 100..499.
    /// </summary>
    [DataField]
    public Dictionary<string, PrisonFamilyMapIdRange> FamilyMapIdRanges { get; init; } = new();
}

/// <summary>
/// Настройки игрового процесса тюрьмы
/// </summary>
[DataRecord]
public sealed partial record PrisonGameplaySettings
{
    [DataField] public int MinPlayersRequired { get; init; } = 2;

    /// <summary>
    /// Квоты ролей для запуска карты (роль -> количество). Роли с приоритетом должны быть заполнены для старта.
    /// </summary>
    [DataField]
    public Dictionary<string, int> RequiredRoles { get; init; } = new()
    {
        {"HeadOfPrison", 1},
        {"PlanetPrisoner", 1}
    };
}
