using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CentCom;

[Serializable, NetSerializable]
public sealed class ShuttleDelay
{
    public string Label;

    public TimeSpan Time;

    public ShuttleDelay(string label, TimeSpan timeSpan)
    {
        Label = label;
        Time = timeSpan;
    }
}

[Serializable, NetSerializable]
public sealed class LinkedStation
{
    // public NetEntity Uid;
    public string Name = string.Empty;
    public string CurrentAlert = string.Empty;
    public List<string> AlertLevels = [];
    public TimeSpan DefaultDelay = TimeSpan.FromMinutes(10);
    public List<ShuttleDelay> Delays = new List<ShuttleDelay>()
    {
        new ShuttleDelay("5min", TimeSpan.FromMinutes(5)),
        new ShuttleDelay("10min", TimeSpan.FromMinutes(10)),
    };
}

[RegisterComponent]
public sealed partial class CentComConsoleComponent : Component
{
    public string IdCardSlotId = "CentCom-IdSlot";

    [DataField]
    public ItemSlot IdSlot = new();

    [DataField]
    public LinkedStation? Station;
}
