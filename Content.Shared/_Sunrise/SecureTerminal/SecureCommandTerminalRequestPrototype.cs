using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.SecureTerminal;

/// <summary>
/// Defines one requestable action in the Secure Command Terminal.
/// </summary>
[Prototype("secureTerminalRequest")]
public sealed partial class SecureCommandTerminalRequestPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Localization key for the display name shown in the request list.</summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>Localization key for the info panel description.</summary>
    [DataField]
    public LocId Description = string.Empty;

    /// <summary>Whether creating this proposal sends a station-wide announcement.</summary>
    [DataField]
    public bool ProposalAnnouncement = true;

    /// <summary>
    /// Localization key for the global announcement sent when all signatures are collected.
    /// </summary>
    [DataField]
    public LocId? Announcement;

    [DataField]
    public Color AnnouncementColor = Color.Orange;

    /// <summary>
    /// If set, this request is a sub-item and will appear indented under the named parent request in the UI.
    /// </summary>
    [DataField]
    public string? ParentId;

    /// <summary>
    /// Seconds to wait after all authorizations are collected before executing the action.
    /// This delay prevents requested support from arriving immediately.
    /// </summary>
    [DataField]
    public int ActivationDelaySecs = 600;

    /// <summary>
    /// Credits reserved from the station's primary account while the request awaits authorization.
    /// </summary>
    [DataField]
    public int Fee = 5000;

    /// <summary>
    /// Fraction of every station bank account removed when this request becomes fully authorized.
    /// A value of 0.05 removes 5 percent of every account balance.
    /// </summary>
    [DataField]
    public float BudgetPenalty = 0.05f;

    /// <summary>
    /// Seconds available to collect every authorization before automatic cancellation.
    /// A value of zero disables this timer.
    /// </summary>
    [DataField]
    public int AuthTimer = 0;

    /// <summary>What type of action to perform when the timer expires.</summary>
    [DataField(required: true)]
    public SecureTerminalActionType ActionType;

    /// <summary>Entity prototype ID of the game rule to start.</summary>
    [DataField]
    public string? GameruleId;

    /// <summary>Alert level key to set.</summary>
    [DataField]
    public string? AlertLevel;

    /// <summary>Armory key to dispatch.</summary>
    [DataField]
    public string? ArmoryKey;

    /// <summary>Access levels affected by an airlock-access action.</summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>>? AllowedAccesses = new();

    /// <summary>Whether the selected airlock access is enabled.</summary>
    [DataField]
    public bool AccessEnabled;

    /// <summary>
    /// List of authorization groups.  Each inner list is a set of access-level prototype IDs;
    /// Any access tag in an inner list satisfies that group. Every group must be
    /// satisfied by a distinct person before the countdown begins.
    /// </summary>
    [DataField(required: true)]
    public List<List<ProtoId<AccessLevelPrototype>>> AuthGroups = new();

    /// <summary>
    /// Human-readable label for each group shown in the Authorization panel.
    /// Must match the length of AuthGroups; falling back to the raw tag names if missing.
    /// </summary>
    [DataField]
    public List<LocId> AuthGroupLabels = new();

    /// <summary>Whether the requester must provide a reason.</summary>
    [DataField]
    public bool RequireReason;

    /// <summary>Whether an administrator must also authorize the request.</summary>
    [DataField]
    public bool RequiresAdminApproval;

    /// <summary>Whether administrator approval is bypassed when no administrators are online.</summary>
    [DataField]
    public bool BypassIfNoAdmin = true;

    /// <summary>If true, the request button is hidden/disabled unless War Ops are active.</summary>
    [DataField]
    public bool RequiresWarDeclared;

    /// <summary>If true, the request button is hidden/disabled when War Ops is active.</summary>
    [DataField]
    public bool RequiresWarNotDeclared;

    /// <summary>If set, requires this alert level to be currently active on the station.</summary>
    [DataField]
    public string? RequiresAlertLevel;

    /// <summary>
    /// If > 0, the alert level specified in RequiresAlertLevel must have been active for at least this many minutes.
    /// </summary>
    [DataField]
    public int RequiresAlertActiveMinutes = 0;

    /// <summary>Minimum seconds from full authorization before the armory recall becomes available. 0 = no delay.</summary>
    [DataField]
    public int RecallMinDelaySecs = 0;

    /// <summary>Seconds before this request can be started again after completion.</summary>
    [DataField]
    public int CooldownSecs = 1800;

    /// <summary>
    /// Display order in the request list. Lower numbers appear first.
    /// Defaults to 100 so unset entries sort to the end.
    /// </summary>
    [DataField]
    public int SortOrder = 100;

    /// <summary>
    /// If true, this request can only be activated once per round.
    /// After use or recall it shows "USED" and cannot be re-requested.
    /// </summary>
    [DataField]
    public bool OneTimeUse;
}

public enum SecureTerminalActionType
{
    GameRule,
    AlertLevel,
    Armory,
    NukeCodes,
    AirlockAccess,
}
