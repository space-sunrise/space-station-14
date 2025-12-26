using System;
using System.Collections.Generic;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
///     Network state for the admin implant management UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class ImplantAdminEuiState : EuiStateBase
{
    public NetEntity TargetNetEntity;
    public List<ImplantEntry> Implants = new();
    // Body part (external limbs) slots
    public List<BodySlotEntry> PartSlots = new();
    // Organ slots
    public List<BodySlotEntry> OrganSlots = new();
}

[Serializable, NetSerializable]
public sealed class ImplantEntry
{
    public NetEntity NetEntity;
    public string ProtoId = string.Empty;
    public string Name = string.Empty;
    public bool Permanent;
}

[Serializable, NetSerializable]
public sealed class BodySlotEntry
{
    public string SlotId = string.Empty; // e.g. "left arm", "heart"
    public bool IsOrgan; // false = body part, true = organ
    public bool Occupied; // whether something is installed
    public NetEntity? Entity; // present if occupied
    public string ProtoId = string.Empty; // prototype of installed entity
    public string Name = string.Empty; // display name of installed entity
}

/// <summary>
/// Request from client to add an implant by prototype ID.
/// </summary>
[Serializable, NetSerializable]
public sealed class ImplantAdminAddMessage : EuiMessageBase
{
    public string ProtoId = string.Empty;
}

/// <summary>
/// Request from client to remove an existing implant (net entity).
/// </summary>
[Serializable, NetSerializable]
public sealed class ImplantAdminRemoveMessage : EuiMessageBase
{
    public NetEntity ImplantNetEntity;
}

/// <summary>
/// Replace (or insert) a body part / organ in a specific slot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ImplantAdminReplaceBodySlotMessage : EuiMessageBase
{
    public string SlotId = string.Empty;
    public bool IsOrgan;
    public string ProtoId = string.Empty; // prototype to spawn & insert
}

/// <summary>
/// Remove an existing body part / organ from a slot (without replacement).
/// </summary>
[Serializable, NetSerializable]
public sealed class ImplantAdminRemoveBodySlotMessage : EuiMessageBase
{
    public string SlotId = string.Empty;
    public bool IsOrgan;
}
