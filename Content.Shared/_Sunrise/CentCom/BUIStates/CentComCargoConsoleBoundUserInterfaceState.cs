using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CentCom.BUIStates;

[Serializable, NetSerializable]
public sealed class CentComCargoConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public NetEntity Owner;
    public readonly CargoLinkedStation? Station;

    public CentComCargoConsoleBoundUserInterfaceState(NetEntity owner, CargoLinkedStation? station)
    {
        Owner = owner;
        Station = station;
    }
}
