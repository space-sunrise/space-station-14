using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NavigatorUiState : BoundUserInterfaceState
{
    public NetEntity? MapUid;
    public string StationName;

    public NavigatorUiState(NetEntity? mapUid, string stationName)
    {
        MapUid = mapUid;
        StationName = stationName;
    }
}