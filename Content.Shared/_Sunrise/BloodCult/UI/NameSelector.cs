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
    public string Name { get; set; }

    public NameSelectorBuiState(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class NameSelectorMessage : BoundUserInterfaceMessage
{
    public string Name { get; set; }

    public NameSelectorMessage(string name)
    {
        Name = name;
    }
}


[NetSerializable, Serializable]
public enum RuneTeleporterUiKey
{
    Key
}

[Serializable, NetSerializable]
public class TeleportRunesListWindowItemSelectedMessage : BoundUserInterfaceMessage
{
    public int SelectedItem { get; private set; }
    public int Index { get; private set; }

    public TeleportRunesListWindowItemSelectedMessage(int selectedItem, int index)
    {
        SelectedItem = selectedItem;
        Index = index;
    }
}

[Serializable, NetSerializable]
public class TeleportRunesListWindowBUIState : BoundUserInterfaceState
{
    public List<int> Items { get; set; }
    public List<string> Label { get; set; }

    public TeleportRunesListWindowBUIState(List<int> items, List<string> labels)
    {
        Items = items;
        Label = labels;
    }
}


[NetSerializable, Serializable]
public enum SummonCultistUiKey
{
    Key
}

[Serializable, NetSerializable]
public class SummonCultistListWindowItemSelectedMessage : BoundUserInterfaceMessage
{
    public int SelectedItem { get; private set; }
    public int Index { get; private set; }

    public SummonCultistListWindowItemSelectedMessage(int selectedItem, int index)
    {
        SelectedItem = selectedItem;
        Index = index;
    }
}

[Serializable, NetSerializable]
public class SummonCultistListWindowBUIState : BoundUserInterfaceState
{
    public List<int> Items { get; set; }
    public List<string> Label { get; set; }

    public SummonCultistListWindowBUIState(List<int> items, List<string> labels)
    {
        Items = items;
        Label = labels;
    }
}


[Serializable, NetSerializable]
public enum SinguloCallUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SinguloCallBuiState : BoundUserInterfaceState
{
    public string Name { get; set; }

    public SinguloCallBuiState(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class SinguloCallMessage : BoundUserInterfaceMessage
{
    public string Name { get; set; }

    public SinguloCallMessage(string name)
    {
        Name = name;
    }
}
