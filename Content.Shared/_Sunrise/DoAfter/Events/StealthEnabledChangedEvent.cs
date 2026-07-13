namespace Content.Shared._Sunrise.DoAfter.Events;

/// <summary>
/// Сигнализирует об изменении фактического состояния стелса сущности.
/// </summary>
public readonly record struct StealthEnabledChangedEvent(bool Enabled);
