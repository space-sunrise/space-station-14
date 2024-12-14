using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Items;

[NetSerializable, Serializable]
public enum CultTeleporterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public class TorchWindowItemSelectedMessage : BoundUserInterfaceMessage
{
    public string SelectedItem { get; private set; }
    public string EntUid { get; private set; }

    public TorchWindowItemSelectedMessage(string entUid, string selectedItem)
    {
        SelectedItem = selectedItem;
        EntUid = entUid;
    }
}

[Serializable, NetSerializable]
public class TorchWindowBUIState : BoundUserInterfaceState
{
    public Dictionary<string, string> Items { get; set; }

    public TorchWindowBUIState(Dictionary<string, string> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public enum VoidTorchVisuals : byte
{
    Activated
}
