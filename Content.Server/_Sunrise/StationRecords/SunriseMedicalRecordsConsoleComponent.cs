using Content.Shared.StationRecords;

namespace Content.Server._Sunrise.StationRecords;

/// <summary>
/// Компонент консоли медицинских записей.
/// </summary>
[RegisterComponent]
public sealed partial class SunriseMedicalRecordsConsoleComponent : Component
{
    public uint? ActiveKey;
    public StationRecordsFilter? Filter;
    public bool HasAccess;
}
