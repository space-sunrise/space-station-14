using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Research.TechnologyDisk;

[Serializable, NetSerializable]
public enum SunriseDiskConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SunriseDiskConsoleBoundUserInterfaceState(
    int serverPoints,
    List<SunriseDiskConsolePrintOption> diskOptions,
    bool isPrinting) : BoundUserInterfaceState
{
    public int ServerPoints = serverPoints;
    public List<SunriseDiskConsolePrintOption> DiskOptions = diskOptions;
    public bool IsPrinting = isPrinting;
}

[Serializable, NetSerializable]
public sealed class SunriseDiskConsolePrintDiskMessage(EntProtoId prototype) : BoundUserInterfaceMessage
{
    public EntProtoId Prototype = prototype;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class SunriseDiskConsolePrintOption
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public int PointCost;

    private SunriseDiskConsolePrintOption()
    {
    }

    public SunriseDiskConsolePrintOption(EntProtoId prototype, int pointCost)
    {
        Prototype = prototype;
        PointCost = pointCost;
    }
}
