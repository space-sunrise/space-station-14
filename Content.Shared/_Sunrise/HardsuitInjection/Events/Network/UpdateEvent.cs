using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.EntitySystems;


[Serializable, NetSerializable]
public sealed class UpdateECEvent(NetEntity beakerUid, Solution solution, FixedPoint2 reagentTransfer) : EntityEventArgs
{
    public NetEntity BeakerUid = beakerUid;
    public Solution Solution = solution;
    public FixedPoint2 ReagentTransfer = reagentTransfer;
    public Solution? RemovedReagentAmount = null;
}
