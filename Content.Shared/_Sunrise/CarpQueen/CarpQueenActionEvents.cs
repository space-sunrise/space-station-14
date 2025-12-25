using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CarpQueen;

public sealed partial class CarpQueenSummonActionEvent : InstantActionEvent
{
}

/// <summary>
/// Event for carp queen order actions (Stay, Follow, Kill, Loose).
/// </summary>
public sealed partial class CarpQueenOrderActionEvent : InstantActionEvent
{
    /// <summary>
    /// The type of order being given
    /// </summary>
    [DataField("type")]
    public CarpQueenOrderType Type;
}

[Serializable, NetSerializable]
public enum CarpQueenOrderType : byte
{
    Stay,
    Follow,
    Kill,
    Loose
}


