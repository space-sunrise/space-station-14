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
public enum DynamicAppearanceFields
{
    None     = 0,
    Name     = 1 << 0,
    Sex      = 1 << 1,
    Pronouns = 1 << 2,
    SkinColor = 1 << 3,
    EyeColor = 1 << 4,
    Hair     = 1 << 5,
    Markings = 1 << 6,

    All = Name | Sex | Pronouns | SkinColor | EyeColor | Hair | Markings,
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
