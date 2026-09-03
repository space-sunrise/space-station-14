using Content.Shared.StationRecords;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.StationRecords;

[Serializable, NetSerializable]
public enum SunriseMedicalRecordsConsoleKey : byte
{
    Key
}

/// <summary>
/// Состояние BUI медицинской консоли.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseMedicalRecordsConsoleState : BoundUserInterfaceState
{
    public readonly uint? SelectedKey;
    public readonly GeneralStationRecord? Record;
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly StationRecordsFilter? Filter;
    public readonly bool HasAccess;

    public SunriseMedicalRecordsConsoleState(
        uint? selectedKey,
        GeneralStationRecord? record,
        Dictionary<uint, string>? recordListing,
        StationRecordsFilter? filter,
        bool hasAccess)
    {
        SelectedKey = selectedKey;
        Record = record;
        RecordListing = recordListing;
        Filter = filter;
        HasAccess = hasAccess;
    }

    public SunriseMedicalRecordsConsoleState() : this(null, null, null, null, false) { }
}

/// <summary>
/// Запрос на печать медицинского досье.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunrisePrintMedicalRecord(uint id) : BoundUserInterfaceMessage
{
    public readonly uint Id = id;
}

/// <summary>
/// Сохранение изменений в медицинском досье.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseSaveMedicalRecord(string medicalRecord, uint id) : BoundUserInterfaceMessage
{
    public readonly string MedicalRecord = medicalRecord;
    public readonly uint Id = id;
}
