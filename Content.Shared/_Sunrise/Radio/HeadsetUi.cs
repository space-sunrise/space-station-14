using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Radio;

[Serializable, NetSerializable]
public enum HeadsetUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class HeadsetToggleChannelMessage(string channelId, bool enabled) : BoundUserInterfaceMessage
{
    public string ChannelId { get; } = channelId;
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class HeadsetChangeVolumeMessage(string channelId, float volume) : BoundUserInterfaceMessage
{
    public string ChannelId { get; } = channelId;
    public float Volume { get; } = volume;
}

[DataDefinition]
public sealed partial class ToggleHeadsetActionEvent : InstantActionEvent { }
