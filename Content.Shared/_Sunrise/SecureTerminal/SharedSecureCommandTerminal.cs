using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SecureTerminal;

/// <summary>
/// Marker component added to communications consoles that have the Secure Command Terminal feature.
/// </summary>
[RegisterComponent]
public sealed partial class SecureCommandTerminalConsoleComponent : Component
{
    /// <summary>
    /// Whether this console can use the Secure Command Terminal interface.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>Whether this console can provide administrator authorization.</summary>
    [DataField]
    public bool Admin = false;

    /// <summary>Whether this is a dedicated authorization terminal.</summary>
    [DataField]
    public bool AuthTerminal = false;
}

/// <summary>Bound interface key for the Secure Command Terminal.</summary>
[Serializable, NetSerializable]
public enum SecureCommandTerminalUiKey { Key }

/// <summary>Lifecycle state of a Secure Command Terminal proposal.</summary>
[Serializable, NetSerializable]
public enum SecureTerminalProposalStatus
{
    Pending,
    Activating,
    Completed,
}

/// <summary>Request / re-propose an action.</summary>
[Serializable, NetSerializable]
public sealed class SecureTerminalRequestMessage : BoundUserInterfaceMessage
{
    /// <summary>Prototype ID of the requested action.</summary>
    public readonly string RequestId;

    /// <summary>Creates a request message for the selected prototype.</summary>
    public SecureTerminalRequestMessage(string requestId) => RequestId = requestId;
}

/// <summary>Authorize / sign the currently pending proposal for a given request.</summary>
[Serializable, NetSerializable]
public sealed class SecureTerminalAuthorizeMessage : BoundUserInterfaceMessage
{
    /// <summary>Prototype ID of the proposal to authorize.</summary>
    public readonly string RequestId;

    /// <summary>Creates an authorization message for the selected proposal.</summary>
    public SecureTerminalAuthorizeMessage(string requestId) => RequestId = requestId;
}

/// <summary>Cancel / deny the currently pending proposal for a given request.</summary>
[Serializable, NetSerializable]
public sealed class SecureTerminalDenyMessage : BoundUserInterfaceMessage
{
    /// <summary>Prototype ID of the proposal to deny.</summary>
    public readonly string RequestId;

    /// <summary>Creates a denial message for the selected proposal.</summary>
    public SecureTerminalDenyMessage(string requestId) => RequestId = requestId;
}

/// <summary>Abort an activating armory during its countdown (free, marks it permanently as used).</summary>
[Serializable, NetSerializable]
public sealed class SecureTerminalRecallMessage : BoundUserInterfaceMessage
{
    /// <summary>Prototype ID of the armory request to recall.</summary>
    public readonly string RequestId;

    /// <summary>Creates a recall message for the selected armory.</summary>
    public SecureTerminalRecallMessage(string requestId) => RequestId = requestId;
}

/// <summary>
/// Net-serializable snapshot of one active proposal, sent to clients.
/// </summary>
[Serializable, NetSerializable]
public sealed class SecureTerminalProposalState
{
    /// <summary>Prototype ID of the request represented by this state.</summary>
    public string RequestId = string.Empty;

    /// <summary>Display name + job-title of each person who has signed.</summary>
    public List<(string Name, string Job)> AuthorizedBy = new();

    /// <summary>True if the corresponding auth-group has been satisfied.</summary>
    public List<bool> GroupsSatisfied = new();

    /// <summary>Human-readable label per auth-group, e.g. "Captain / HoS".</summary>
    public List<string> GroupLabels = new();

    /// <summary>
    /// When the action will fire (CurTime, server-side).
    /// Null while still gathering authorizations.
    /// </summary>
    public TimeSpan? ActivateAt;

    /// <summary>CurTime when an incomplete proposal expires.</summary>
    public TimeSpan? AuthTimer;

    /// <summary>Current proposal lifecycle status.</summary>
    public SecureTerminalProposalStatus Status;
}

/// <summary>
/// Server-authoritative snapshot displayed by the Secure Command Terminal UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class SecureCommandTerminalInterfaceState : BoundUserInterfaceState
{
    public readonly List<SecureTerminalProposalState> Proposals;
    public readonly bool IsWarDeclared;
    /// <summary>RequestId → cooldown end time (CurTime). Entries disappear when cooldown expires.</summary>
    public readonly Dictionary<string, TimeSpan> CoolingDown;
    /// <summary>Current station alert level id (e.g. "gamma"), or null if unknown.</summary>
    public readonly string? CurrentAlertLevel;
    /// <summary>One-time-use request IDs permanently consumed this round.</summary>
    public readonly HashSet<string> UsedOnce;
    /// <summary>When the current alert level was last set (CurTime).</summary>
    public readonly TimeSpan AlertLevelSetAt;
    /// <summary>Armory requests currently deployed (fired but not recalled). Maps requestId → authorization time for delay UI.</summary>
    public readonly Dictionary<string, TimeSpan> DeployedArmories;

    /// <summary>Creates a complete Secure Command Terminal UI snapshot.</summary>
    public SecureCommandTerminalInterfaceState(
        List<SecureTerminalProposalState> proposals,
        bool isWarDeclared,
        Dictionary<string, TimeSpan> coolingDown,
        string? currentAlertLevel,
        HashSet<string> usedOnce,
        TimeSpan alertLevelSetAt,
        Dictionary<string, TimeSpan> deployedArmories)
    {
        Proposals = proposals;
        IsWarDeclared = isWarDeclared;
        CoolingDown = coolingDown;
        CurrentAlertLevel = currentAlertLevel;
        UsedOnce = usedOnce;
        AlertLevelSetAt = alertLevelSetAt;
        DeployedArmories = deployedArmories;
    }
}
