using Content.Shared._Sunrise.SecureTerminal;

namespace Content.Server._Sunrise.SecureTerminal;

/// <summary>
/// Tracks all Secure Command Terminal proposals for a station.
/// </summary>
[RegisterComponent]
public sealed partial class SecureCommandTerminalStationComponent : Component
{
    /// <summary>Active proposals keyed by RequestId.</summary>
    [ViewVariables]
    public readonly Dictionary<string, SecureTerminalProposalData> ActiveProposals = new();

    /// <summary>Per-request cooldown end times (CurTime).</summary>
    [ViewVariables]
    public readonly Dictionary<string, TimeSpan> Cooldowns = new();

    /// <summary>One-time-use request IDs permanently consumed this round.</summary>
    [ViewVariables]
    public readonly HashSet<string> UsedOnce = [];

    /// <summary>Armory requests that have fired and are deployed. Maps requestId → time of authorization (for recall delay). Removed when recalled.</summary>
    [ViewVariables]
    public readonly Dictionary<string, TimeSpan> DeployedArmories = new();

    /// <summary>Requester of each deployed armory.</summary>
    [ViewVariables]
    public readonly Dictionary<string, EntityUid> DeployedArmoryRequesters = new();

    /// <summary>When the current alert level was last set (CurTime). Used for RequiresAlertActiveMinutes checks.</summary>
    [ViewVariables]
    public TimeSpan AlertLevelSetAt;
}

/// <summary>Server-only live data for one pending/activating proposal.</summary>
public sealed class SecureTerminalProposalData
{
    /// <summary>Prototype ID of the requested action.</summary>
    public string RequestId = string.Empty;

    /// <summary>The player who created the request.</summary>
    public EntityUid Requester = EntityUid.Invalid;

    /// <summary>The station whose account supplied the held request fee.</summary>
    public EntityUid Station = EntityUid.Invalid;

    /// <summary>Reason supplied by the requester.</summary>
    public string Reason = string.Empty;

    /// <summary>Whether an administrator authorized the proposal.</summary>
    public bool AdminApproved = false;

    /// <summary>
    /// Each entry: PlayerUid, display name, job name, which auth-group index they satisfy.
    /// </summary>
    public readonly List<(EntityUid PlayerUid, string Name, string Job, int GroupIndex)> Authorizers = new();

    /// <summary>Dedicated authorization terminals already used for this proposal.</summary>
    public readonly List<EntityUid> UsedTerminals = new();

    /// <summary>CurTime when the action fires. Null while still collecting signatures.</summary>
    public TimeSpan? ActivateAt;

    /// <summary>CurTime when an incomplete proposal expires.</summary>
    public TimeSpan? AuthTimer;

    /// <summary>Current proposal lifecycle status.</summary>
    public SecureTerminalProposalStatus Status = SecureTerminalProposalStatus.Pending;
}
