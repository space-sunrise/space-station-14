using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LockableEquipment;

[Serializable, NetSerializable]
public sealed class EquipmentVisualData(bool visible, string? layer, string? rsiPath, string? state) : ICloneable
{
    public readonly bool Visible = visible;
    public readonly string? Layer = layer;
    public readonly string? RsiPath = rsiPath;
    public readonly string? State = state;

    public object Clone()
    {
        return new EquipmentVisualData(Visible, Layer, RsiPath, State);
    }
}
