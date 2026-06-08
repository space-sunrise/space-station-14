using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CarpQueen;

public sealed partial class CarpQueenSummonActionEvent : InstantActionEvent
{
}

/// <summary>
/// Событие для действий приказа королевы карпов (Stay, Follow, Kill, Loose).
/// </summary>
public sealed partial class CarpQueenOrderActionEvent : InstantActionEvent
{
    /// <summary>
    /// Тип отдаваемого приказа.
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

