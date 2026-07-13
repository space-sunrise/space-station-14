namespace Content.Shared._Sunrise.DoAfter.Events;

/// <summary>
/// Signals that an entity's active stealth state changed.
/// </summary>
public readonly record struct StealthEnabledChangedEvent(bool Enabled);
