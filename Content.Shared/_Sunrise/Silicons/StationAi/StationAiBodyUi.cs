using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Silicons.StationAi;

[Serializable, NetSerializable]
public enum StationAiBodyUiKey : byte
{
    /// <summary>
    /// Body selector interface on the station AI brain entity.
    /// </summary>
    Key
}

/// <summary>
/// Server-authoritative station AI body selector state.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiBodyBuiState(List<StationAiBodyBuiEntry> bodies, NetEntity? currentBody) : BoundUserInterfaceState
{
    public List<StationAiBodyBuiEntry> Bodies { get; } = bodies;
    public NetEntity? CurrentBody { get; } = currentBody;
}

/// <summary>
/// One station AI body entry displayed in the body selector.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiBodyBuiEntry(
    NetEntity body,
    int bodyNumber,
    string name,
    NetEntity? linkedAi,
    bool isCurrent)
{
    public NetEntity Body { get; } = body;
    public int BodyNumber { get; } = bodyNumber;
    public string Name { get; } = name;
    public NetEntity? LinkedAi { get; } = linkedAi;
    public bool IsCurrent { get; } = isCurrent;
}

/// <summary>
/// Requests station AI transfer into the selected free body.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiBodyEnterMessage(NetEntity body) : BoundUserInterfaceMessage
{
    public NetEntity Body { get; } = body;
}

/// <summary>
/// Requests manual return from the current body to the station AI brain.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiBodyExitMessage : BoundUserInterfaceMessage;
