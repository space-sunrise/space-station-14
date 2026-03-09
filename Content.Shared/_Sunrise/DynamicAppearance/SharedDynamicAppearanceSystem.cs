using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.DynamicAppearance;

[Serializable, NetSerializable]
public enum DynamicAppearanceUiKey
{
    Key,
}

/// <summary>
/// Flags indicating which appearance fields a player is allowed to edit in <see cref="DynamicAppearanceComponent"/>.
/// </summary>
[Flags]
[Serializable, NetSerializable]
public enum DynamicAppearanceFields : uint
{
    None = 0,
    Name = 1 << 0,
    Age = 1 << 1,
    Size = 1 << 2,
    Sex = 1 << 3,
    BodyType = 1 << 4,
    Pronouns = 1 << 5,
    SkinColor = 1 << 6,
    EyeColor = 1 << 7,
    Hair = 1 << 8,
    Markings = 1 << 9,
    Species = 1 << 10,
    Voice = 1 << 11,

    All = Name | Age | Size | Sex | BodyType | Pronouns | SkinColor | EyeColor | Hair | Markings | Species | Voice,
}

/// <summary>
/// Complete snapshot of the editable appearance fields.
/// Used in BUI messages, BUI state, and as the local draft in the editor window.
/// </summary>
[Serializable, NetSerializable]
public record struct DynamicAppearanceState(
    MarkingSet MarkingSet,
    string Species,
    Sex Sex,
    int Age,
    Gender Gender,
    string Voice,
    Color SkinColor,
    Color EyeColor,
    Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers,
    string BodyType,
    float Width,
    float Height,
    string Name
);

/// <summary>
/// Client → Server: commit the complete appearance draft.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceSaveMessage : BoundUserInterfaceMessage
{
    public DynamicAppearanceState State { get; }

    public DynamicAppearanceSaveMessage(DynamicAppearanceState state)
    {
        State = state;
    }
}

/// <summary>
/// Client → Server: toggles the admin-only restriction override for the current editor session.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceSetAdminOverrideMessage : BoundUserInterfaceMessage
{
    public bool Enabled { get; }

    public DynamicAppearanceSetAdminOverrideMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

/// <summary>
/// Server → Client: actor-specific permission overlay for the appearance editor.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearancePermissionsMessage : BoundUserInterfaceMessage
{
    public bool CanOverrideRestrictions { get; }
    public bool OverrideRestrictions { get; }

    public DynamicAppearancePermissionsMessage(bool canOverrideRestrictions, bool overrideRestrictions)
    {
        CanOverrideRestrictions = canOverrideRestrictions;
        OverrideRestrictions = overrideRestrictions;
    }
}

/// <summary>
/// Server → Client: full appearance snapshot + the entity being edited (for preview) +
/// which fields the player is permitted to edit.
/// </summary>
[Serializable, NetSerializable]
public sealed class DynamicAppearanceBUIState : BoundUserInterfaceState
{
    public DynamicAppearanceState State { get; }

    /// <summary>
    /// The entity whose appearance is being edited. Used by the client to display a live preview.
    /// </summary>
    public NetEntity Entity { get; }

    /// <summary>
    /// Bitmask of appearance fields the player is allowed to edit.
    /// The client should disable/hide controls for fields not in this set.
    /// </summary>
    public DynamicAppearanceFields AllowedFields { get; }

    public DynamicAppearanceBUIState(DynamicAppearanceState state, NetEntity entity, DynamicAppearanceFields allowedFields)
    {
        State = state;
        Entity = entity;
        AllowedFields = allowedFields;
    }
}
