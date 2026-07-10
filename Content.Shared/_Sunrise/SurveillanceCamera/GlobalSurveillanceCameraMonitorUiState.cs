using Content.Shared.SurveillanceCamera;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SurveillanceCamera;

[Serializable, NetSerializable]
public sealed class GlobalSurveillanceCameraMonitorUiState : BoundUserInterfaceState
{
    public NetEntity? ActiveCamera { get; }
    public HashSet<string> Subnets { get; }
    public string ActiveAddress { get; }
    public Dictionary<NetEntity, CameraData> Cameras { get; }
    public NetEntity? BackgroundGrid { get; }

    public GlobalSurveillanceCameraMonitorUiState(
        NetEntity? activeCamera,
        HashSet<string> subnets,
        string activeAddress,
        Dictionary<NetEntity, CameraData> cameras,
        NetEntity? backgroundGrid)
    {
        ActiveCamera = activeCamera;
        Subnets = subnets ?? new HashSet<string>();
        ActiveAddress = activeAddress ?? string.Empty;
        Cameras = cameras ?? new Dictionary<NetEntity, CameraData>();
        BackgroundGrid = backgroundGrid;
    }
}
