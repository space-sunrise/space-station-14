using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Shipyard.Events;

[Serializable, NetSerializable]
public sealed class ShipyardConsolePurchaseMessage : BoundUserInterfaceMessage
{
    public string VesselId;

    public ShipyardConsolePurchaseMessage(string vesselId)
    {
        VesselId = vesselId;
    }
}
