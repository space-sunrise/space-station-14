using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LockableEquipment;

[Serializable, NetSerializable]
public sealed class EquipmentVisualData : ICloneable
{
    public readonly bool Visible;
    public readonly string? Layer;
    public readonly string? RsiPath;
    public readonly string? State;

    public EquipmentVisualData(bool visible, string? layer, string? rsiPath, string? state)
    {
        Visible = visible;
        Layer = layer;
        RsiPath = rsiPath;
        State = state;
    }

    public object Clone()
    {
        return new EquipmentVisualData(Visible, Layer, RsiPath, State);
    }
}
