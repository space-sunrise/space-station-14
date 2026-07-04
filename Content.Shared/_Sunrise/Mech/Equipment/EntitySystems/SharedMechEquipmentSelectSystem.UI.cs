using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Mech.Equipment.EntitySystems;

/// <summary>
/// Sent when an active mech equipment has been selected.
/// </summary>
[Serializable, NetSerializable]
public sealed class MechActiveEquipmentSelectMessage(NetEntity? selectedEquipment) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the equipment to select.
    /// </summary>
    public readonly NetEntity? SelectedEquipment = selectedEquipment;
}

[Serializable, NetSerializable]
public enum MechEquipmentSelectUiKey : byte
{
    Key,
}
