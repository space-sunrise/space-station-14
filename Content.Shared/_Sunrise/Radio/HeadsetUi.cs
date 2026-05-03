using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Radio;

[Serializable, NetSerializable]
public enum HeadsetUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class HeadsetToggleChannelMessage : BoundUserInterfaceMessage
{
    public string ChannelId { get; }
    public bool Enabled { get; }

    public HeadsetToggleChannelMessage(string channelId, bool enabled)
    {
        ChannelId = channelId;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class HeadsetChangeVolumeMessage : BoundUserInterfaceMessage
{
    public string ChannelId { get; }
    public float Volume { get; }

    public HeadsetChangeVolumeMessage(string channelId, float volume)
    {
        ChannelId = channelId;
        Volume = volume;
    }
}

[DataDefinition]
public sealed partial class ToggleHeadsetActionEvent : InstantActionEvent { }
