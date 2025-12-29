using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.StationRecords;

[Serializable, NetSerializable]
public sealed class SaveStationRecord(GeneralStationRecord record, uint id) : BoundUserInterfaceMessage
{
    public readonly uint Id = id;
    public readonly GeneralStationRecord Record = record;
}

[Serializable, NetSerializable]
public sealed class PrintStationRecord(uint id) : BoundUserInterfaceMessage
{
    public readonly uint Id = id;
}
