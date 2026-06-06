using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Sunrise.Storyteller.Components;

/// <summary>
/// Core component attached to the active Storyteller gamerule. Tracks stress, pacing states, threat budgets, and timers.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StorytellerRuleComponent : Component
{
    /// <summary>
    /// The current stress rating of the crew (0 to 100).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CrewStress = 0f;

    /// <summary>
    /// Current threat budget available to spend on challenging events.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ThreatBudget = 20f;

    /// <summary>
    /// Current major threat budget available to spend on major antag/calm events.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MajorThreatBudget = 30f;

    /// <summary>
    /// Maximum threat budget the storyteller can accumulate.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxThreatBudget = 250f;

    /// <summary>
    /// How much threat budget is generated per second by default.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseBudgetPerSecond = 0.1f;

    /// <summary>
    /// Current pacing state.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public StorytellerPacingState PacingState { get; set; } = StorytellerPacingState.BuildUp;

    /// <summary>
    /// The timestamp when the storyteller should evaluate the station state next.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextCheckTime;

    /// <summary>
    /// Last timestamp when any event was triggered.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastAnyEventTime;

    /// <summary>
    /// Last timestamp when a Helpful event was triggered.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastHelpfulEventTime;

    /// <summary>
    /// Last timestamp when a Neutral event was triggered.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastNeutralEventTime;

    /// <summary>
    /// Global cooldown between any storyteller events (in minutes).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float GlobalEventCooldownMinutes = 3f;

    /// <summary>
    /// Cooldown between Helpful events (in minutes).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HelpfulEventCooldownMinutes = 8f;

    /// <summary>
    /// Cooldown between Neutral events (in minutes).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float NeutralEventCooldownMinutes = 6f;

    /// <summary>
    /// The timestamp when the current pacing state is scheduled to transition.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StateTransitionTime;

    /// <summary>
    /// Game rules executed by the storyteller that are currently active.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> ActiveStorytellerRules = new();

    /// <summary>
    /// Historical list of rules triggered by the storyteller during this round.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<string> EventHistory = new();

    /// <summary>
    /// The active type of the storyteller.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public StorytellerType StorytellerType { get; set; } = StorytellerType.Classic;

    /// <summary>
    /// The configured storyteller type from the game rule entity.
    /// If set, this type will be used instead of the default.
    /// </summary>
    [DataField("storyTellerType")]
    public StorytellerType? ConfiguredStorytellerType;

    /// <summary>
    /// The timestamp when the storyteller rule was started.
    /// </summary>
    [AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RuleStartTime;

    /// <summary>
    /// Alert level history per station, used to calculate stress based on station codes.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, List<AlertLevelHistoryEntry>> AlertLevelHistory = new();
}

public sealed class AlertLevelHistoryEntry
{
    public TimeSpan Time;
    public string Level = string.Empty;
}

public enum StorytellerPacingState
{
    Relaxation, // 0 - Peace and quiet. Only minor positive/neutral events.
    BuildUp,    // 1 - Growing tension. Minor and major calm events, minor antags.
    Peak,       // 2 - Climax. Major threats active. No new major threats spawned.
    Recovery    // 3 - Post-threat recovery. Silent or helpful events only.
}

public enum StorytellerType
{
    Calm,
    Classic,
    Insane
}

