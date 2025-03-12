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
    public ListViewBUIState(List<string> items)
    {
        Items = items;
    }

    public List<string> Items { get; set; }
}

[Serializable, NetSerializable]
public class ListViewItemSelectedMessage : BoundUserInterfaceMessage
{
    public ListViewItemSelectedMessage(string selectedItem, int index)
    {
        SelectedItem = selectedItem;
        Index = index;
    }

    public string SelectedItem { get; private set; }
    public int Index { get; private set; }
}
