using Robust.Shared.Serialization;

namespace Content.Shared._Scp.DeviceLinking;

[Serializable, NetSerializable]
public sealed class DeviceLinkOverlayToggledEvent(bool isEnabled) : EntityEventArgs
{
    public readonly bool IsEnabled = isEnabled;
}

[Serializable, NetSerializable]
public sealed class DeviceLinkOverlayData(List<DebugEntityConnectionData> rays) : EntityEventArgs
{
    public readonly List<DebugEntityConnectionData> Rays = rays;
}

[Serializable, NetSerializable]
public readonly record struct DebugEntityConnectionData(NetEntity Source, List<NetEntity> Connections) { };
