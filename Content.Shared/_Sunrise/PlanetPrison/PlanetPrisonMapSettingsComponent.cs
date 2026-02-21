namespace Content.Shared._Sunrise.PlanetPrison;

/// <summary>
/// Компонент для переопределения настроек тюрьмы в конкретном прототипе карты.
/// Если этот компонент присутствует на станции карты, его настройки имеют приоритет над глобальными настройками из PlanetPrisonSharedComponent.
/// </summary>
[RegisterComponent]
public sealed partial class PlanetPrisonMapSettingsComponent : Component
{
    /// <summary>
    /// Переопределение настроек игрового процесса для этой карты.
    /// Если null, используются глобальные настройки.
    /// </summary>
    [DataField]
    public PrisonGameplaySettings? GameplaySettingsOverride;

    /// <summary>
    /// Переопределение настроек завершения карты для этой карты.
    /// Если null, используются настройки из прототипа карты или глобальные.
    /// </summary>
    [DataField]
    public PrisonMapCompletionSettings? CompletionSettingsOverride;
}
