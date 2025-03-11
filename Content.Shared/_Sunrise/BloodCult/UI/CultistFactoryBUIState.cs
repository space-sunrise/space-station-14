using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.UI;

[Serializable, NetSerializable]
public class CultistFactoryBUIState : BoundUserInterfaceState
{
    public CultistFactoryBUIState(IReadOnlyCollection<string> ids)
    {
        Ids = ids;
    }

    public IReadOnlyCollection<string> Ids { get; set; }
}

[Serializable, NetSerializable]
public class CultistFactoryItemSelectedMessage : BoundUserInterfaceMessage
{
    public CultistFactoryItemSelectedMessage(string item)
    {
        Item = item;
    }

    public string Item { get; private set; }
}
