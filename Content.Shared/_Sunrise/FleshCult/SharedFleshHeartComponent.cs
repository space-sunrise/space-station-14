using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.FleshCult
{
    [NetworkedComponent()]
    [Virtual]
    public partial class SharedFleshHeartComponent : Component
    {
        [DataField("finalState")]
        public string? FinalState = "underpowered";
    }

    [Serializable, NetSerializable]
    public sealed partial class FleshHeartDragFinished : SimpleDoAfterEvent
    {
    }
}
