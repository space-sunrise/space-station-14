using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LockableEquipment;

[Serializable, NetSerializable]
public sealed class LockableEquipmentComponentState : ComponentState
{
    public readonly bool Locked;
    public readonly string? LockId;
    public readonly string Layer;
    public readonly string RsiPath;
    public readonly string SpriteState;

    public LockableEquipmentComponentState(bool locked, string? lockId, string layer, string rsiPath, string spriteState = "equipped")
    {
        Locked = locked;
        LockId = lockId;
        Layer = layer;
        RsiPath = rsiPath;
        SpriteState = spriteState;
    }
}
