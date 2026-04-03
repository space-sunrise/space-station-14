using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SiliconStanding;

[Serializable, NetSerializable]
public sealed partial class SiliconRestingDoAfterEvent : SimpleDoAfterEvent
{
    public bool Success;
}