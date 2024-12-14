using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.UI;

[Serializable, NetSerializable]
public enum ListViewSelectorUiKey
{
    Key
}

[Serializable, NetSerializable]
public class ListViewBUIState : BoundUserInterfaceState
{
    public List<string> Items { get; set; }

    public ListViewBUIState(List<string> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public class ListViewItemSelectedMessage : BoundUserInterfaceMessage
{
    public string SelectedItem { get; private set; }
    public int Index { get; private set; }

    public ListViewItemSelectedMessage(string selectedItem, int index)
    {
        SelectedItem = selectedItem;
        Index = index;
    }
}
