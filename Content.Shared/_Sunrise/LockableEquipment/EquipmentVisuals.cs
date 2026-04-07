using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.LockableEquipment;

[Serializable, NetSerializable]
public enum EquipmentVisuals
{
    Visible,
    Layer,
    Sprite
}
