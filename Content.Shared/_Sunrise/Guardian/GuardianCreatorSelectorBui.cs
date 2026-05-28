using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Guardian;

[Serializable, NetSerializable]
public enum GuardianCreatorSelectorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GuardianCreatorSelectorBuiState(
    List<GuardianCreatorSelectorEntryState> options,
    int selectedIndex)
    : BoundUserInterfaceState
{
    public readonly List<GuardianCreatorSelectorEntryState> Options = options;
    public readonly int SelectedIndex = selectedIndex;
}

[Serializable, NetSerializable]
public sealed class GuardianCreatorSelectorEntryState(
    string prototype,
    string name,
    string description,
    string details)
{
    public readonly string Prototype = prototype;
    public readonly string Name = name;
    public readonly string Description = description;
    public readonly string Details = details;
}

[Serializable, NetSerializable]
public sealed class GuardianCreatorSelectorConfirmMessage(string prototype) : BoundUserInterfaceMessage
{
    public readonly string Prototype = prototype;
}
