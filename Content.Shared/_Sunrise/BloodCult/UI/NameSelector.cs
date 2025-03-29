using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.UI;

[Serializable, NetSerializable]
public enum NameSelectorUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NameSelectorBuiState : BoundUserInterfaceState
{
    public NameSelectorBuiState(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

[Serializable, NetSerializable]
public sealed class NameSelectorMessage : BoundUserInterfaceMessage
{
    public NameSelectorMessage(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

[NetSerializable, Serializable]
public enum RuneTeleporterUiKey
{
    Key
}

[Serializable, NetSerializable]
public class TeleportRunesListWindowItemSelectedMessage : BoundUserInterfaceMessage
{
    public TeleportRunesListWindowItemSelectedMessage(int selectedItem, int index)
    {
        SelectedItem = selectedItem;
        Index = index;
    }

    public int SelectedItem { get; private set; }
    public int Index { get; private set; }
}

[Serializable, NetSerializable]
public class TeleportRuneChangeNameMessage : BoundUserInterfaceMessage
{
    public TeleportRuneChangeNameMessage()
    {
    }
}

[Serializable, NetSerializable]
public class TeleportRunesListWindowBUIState : BoundUserInterfaceState
{
    public TeleportRunesListWindowBUIState(List<int> items, List<string> labels)
    {
        Items = items;
        Label = labels;
    }

    public List<int> Items { get; set; }
    public List<string> Label { get; set; }
}

[NetSerializable, Serializable]
public enum SummonCultistUiKey
{
    Key
}

[Serializable, NetSerializable]
public class SummonCultistListWindowItemSelectedMessage : BoundUserInterfaceMessage
{
    public SummonCultistListWindowItemSelectedMessage(int selectedItem, int index)
    {
        SelectedItem = selectedItem;
        Index = index;
    }

    public int SelectedItem { get; private set; }
    public int Index { get; private set; }
}

[Serializable, NetSerializable]
public class SummonCultistListWindowBUIState : BoundUserInterfaceState
{
    public SummonCultistListWindowBUIState(List<int> items, List<string> labels)
    {
        Items = items;
        Label = labels;
    }

    public List<int> Items { get; set; }
    public List<string> Label { get; set; }
}

[Serializable, NetSerializable]
public enum SinguloCallUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SinguloCallBuiState : BoundUserInterfaceState
{
    public SinguloCallBuiState(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

[Serializable, NetSerializable]
public sealed class SinguloCallMessage : BoundUserInterfaceMessage
{
    public SinguloCallMessage(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}
