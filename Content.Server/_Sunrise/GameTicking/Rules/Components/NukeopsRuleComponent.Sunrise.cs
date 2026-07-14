using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

public sealed partial class NukeopsRuleComponent
{
    /// <summary>
    /// Changes the alert level on all stations with the nuke disk
    /// if null, the alert level will not change.
    /// </summary>
    [DataField]
    public string? SetAlertlevel = "gamma";

    /// <summary>
    /// How many seconds after the declaration of war, the alert level will change.
    /// </summary>
    [DataField]
    public TimeSpan AlertLevelDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// If true, the game will automatically set the alert level after the specified time.
    /// </summary>
    [DataField]
    public bool CanChangeAlertLevel;

    [DataField, AutoPausedField]
    public TimeSpan AlertLevelChangeTime;
}
