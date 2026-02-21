using Content.Shared._Sunrise.PlanetPrison;
using Robust.Shared.Map;

namespace Content.Shared.Maps;

/// <summary>
/// Расширение GameMapPrototype для карт тюрьмы: семейство (UI), размер кэша, режим завершения.
/// Все поля опциональны/с дефолтами, чтобы обычные gameMap не требовали их указывать.
/// </summary>
public sealed partial class GameMapPrototype
{
    /// <summary>
    /// Семейство карты для группировки в UI (например Metus, Nox). null = не тюремная или без группы.
    /// </summary>
    [DataField("prisonMapFamily")]
    public string? PrisonMapFamily { get; private set; }

    /// <summary>
    /// Сколько инстансов этой карты прелоадить в кэш.
    /// </summary>
    [DataField("prisonCacheSize")]
    public int PrisonCacheSize { get; private set; } = 0;

    /// <summary>
    /// Начало диапазона MapId для кэша этой карты. При задании кэши получают MapId = start, start+1, … start+prisonCacheSize-1. null = искать по семейству или глобально.
    /// </summary>
    [DataField("prisonCacheMapIdStart")]
    public int? PrisonCacheMapIdStart { get; private set; }

    /// <summary>
    /// Режим завершения: Freeze — заморозить и удалить при рестарте; Delete — сразу удалить.
    /// </summary>
    [DataField("prisonCompletionMode")]
    public PrisonMapCompletionMode PrisonCompletionMode { get; private set; } = PrisonMapCompletionMode.Delete;
}
