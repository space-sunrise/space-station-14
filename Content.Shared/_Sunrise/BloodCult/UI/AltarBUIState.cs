using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.UI;

[Serializable, NetSerializable]
public enum CultistAltarUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class AltarListingBUIState : BoundUserInterfaceState
{
    public List<string> Items = new();

    public AltarListingBUIState(List<string> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class AltarTimerBUIState : BoundUserInterfaceState
{
    public TimeSpan? NextTimeUse;

    public AltarTimerBUIState(TimeSpan? nextTimeUse)
    {
        NextTimeUse = nextTimeUse;
    }
}

[Serializable, NetSerializable]
public sealed class AltarBuyRequest : BoundUserInterfaceMessage
{
    public string Item = default!;

    public AltarBuyRequest(string item)
    {
        Item = item;
    }
}
