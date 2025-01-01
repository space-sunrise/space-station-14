using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.FleshCult;

[Serializable, NetSerializable]
public enum FleshHeartVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum FleshHeartStatus
{
    Active,
    Disable,
    Destruction
}

[Serializable, NetSerializable]
public enum FleshHeartLayers : byte
{
    Base
}
