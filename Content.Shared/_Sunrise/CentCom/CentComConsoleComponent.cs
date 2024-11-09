using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CentCom;



[Serializable, NetSerializable]
public sealed class CentComConsoleCallEmergencyShuttleMessage : BoundUserInterfaceMessage
{
    public TimeSpan Time;

    public CentComConsoleCallEmergencyShuttleMessage(TimeSpan time)
    {
        Time = time;
    }
}

[Serializable, NetSerializable]
public sealed class CentComConsoleRecallEmergencyShuttleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CentComConsoleAnnounceMessage : BoundUserInterfaceMessage
{
    public string Message;

    public CentComConsoleAnnounceMessage(string msg)
    {
        Message = msg;
    }
}


[Serializable, NetSerializable]
public sealed class CentComConsoleAlertLevelChangeMessage : BoundUserInterfaceMessage
{
    public string TargetLevel;

    public CentComConsoleAlertLevelChangeMessage(string targetLevel)
    {
        TargetLevel = targetLevel;
    }
}

[Serializable, NetSerializable]
public sealed class CentComCargoSendGiftMessage : BoundUserInterfaceMessage
{
    public string TargetGift;

    public CentComCargoSendGiftMessage(string targetGift)
    {
        TargetGift = targetGift;
    }
}

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
    public NetEntity Uid;
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

[Serializable, NetSerializable]
public sealed class CargoLinkedStation
{
    public NetEntity Uid;
    public List<string> AcceptableGifts = [];
}

[RegisterComponent]
public sealed partial class CentComConsoleComponent : Component
{
    public static string IdCardSlotId = "CentComConsole-IdSlot";

    [DataField]
    public ItemSlot IdSlot = new();

    [DataField]
    public LinkedStation? Station;

    [DataField]
    public EntityUid StationUid;

    [DataField]
    public string TargetAccess = "CentralCommand";

    [DataField]
    public string AnnounceVoice = "Villager";

    [DataField]
    public HashSet<string> BlackListAlertLevels = new HashSet<string>()
    {
        "epsilon",
    };

    /// <summary>
    /// Announce sound file path
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Sunrise/Announcements/centcomm.ogg");
}
