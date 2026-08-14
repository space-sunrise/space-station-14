using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Antags.Vampires.UI;

[Serializable, NetSerializable]
public sealed partial class VampireClassClosedBuiMsg : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public enum VampireClassUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum VampireLocateUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VampireClassChosenBuiMsg : BoundUserInterfaceMessage
{
    public string Choice { get; init; } = string.Empty;
}

[Serializable, NetSerializable]
public readonly record struct VampireLocateTarget(NetEntity Target, string DisplayName);

[Serializable, NetSerializable]
public sealed partial class VampireLocateBuiState : BoundUserInterfaceState
{
    public List<VampireLocateTarget> Targets { get; init; } = new();
}

[Serializable, NetSerializable]
public sealed partial class VampireLocateSelectedBuiMsg : BoundUserInterfaceMessage
{
    public NetEntity Target { get; init; }
}
