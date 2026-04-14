using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Sandbox;

[Serializable, NetSerializable]
public sealed class DeviceLinkOverlayToggledEvent(bool isEnabled) : EntityEventArgs
{
    public readonly bool IsEnabled = isEnabled;
}

[Serializable, NetSerializable]
public sealed class DeviceLinkOverlayDataEvent(List<DebugEntityConnectionData> rays) : EntityEventArgs
{
    public readonly List<DebugEntityConnectionData> Rays = rays;
}

[Serializable, NetSerializable]
public readonly record struct DebugEntityConnectionData(NetEntity Source, List<NetEntity> Connections);
