namespace Content.Server._Sunrise.Messenger;

/// <summary>
/// Component for handling messenger spam timing on a station/entity.
/// </summary>
[RegisterComponent]
public sealed partial class StationMessengerSpamComponent : Component
{
    /// <summary>
    /// Current timer in seconds.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Timer;

    /// <summary>
    /// Target time in seconds for the next spam wave.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float NextSpamTime;
}
