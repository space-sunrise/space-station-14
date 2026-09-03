using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CriminalRecords;

/// <summary>
/// Запрос на распечатку охранного досье из консоли криминальных записей.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunrisePrintCriminalRecord(uint id) : BoundUserInterfaceMessage
{
    public readonly uint Id = id;
}
