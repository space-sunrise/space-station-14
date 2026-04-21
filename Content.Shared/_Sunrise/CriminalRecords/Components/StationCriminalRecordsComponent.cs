using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CriminalRecords.Components;

/// <summary>
///     Attached to the station entity to store all Criminal Records.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationCriminalRecordsComponent : Component
{
    /// <summary>
    ///     Collection of all criminal records for this station.
    ///     Key is the uint ID from StationRecordsSystem.
    /// </summary>
    [DataField]
    public Dictionary<uint, List<CriminalCase>> Records = new();

    /// <summary>
    ///     Next available case ID for each person's record.
    /// </summary>
    [DataField]
    public Dictionary<uint, uint> NextCaseIds = new();
}
