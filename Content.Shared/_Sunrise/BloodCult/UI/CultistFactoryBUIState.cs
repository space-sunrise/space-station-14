using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.UI;

[Serializable, NetSerializable]
public class CultistFactoryBUIState : BoundUserInterfaceState
{
    public IReadOnlyCollection<string> Ids { get; set; }

    public CultistFactoryBUIState(IReadOnlyCollection<string> ids)
    {
        Ids = ids;
    }
}

[Serializable, NetSerializable]
public class CultistFactoryItemSelectedMessage : BoundUserInterfaceMessage
{
    public string Item { get; private set; }

    public CultistFactoryItemSelectedMessage(string item)
    {
        Item = item;
    }
}
