namespace Content.Server._Sunrise.GameTicking.Rules.Components;

/// <summary>
/// Sends grids loaded by a game rule to a station through FTL.
/// </summary>
[RegisterComponent, Access(typeof(FTLToStationRuleSystem))]
public sealed partial class FTLToStationRuleComponent : Component
{
    /// <summary>
    /// Dock priority tag used when selecting a station dock.
    /// </summary>
    [DataField]
    public string? PriorityTag;

    /// <summary>
    /// Optional duration of FTL travel in seconds.
    /// </summary>
    [DataField]
    public float? HyperspaceTime;
}
