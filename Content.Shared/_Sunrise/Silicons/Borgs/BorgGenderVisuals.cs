using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Sunrise.Silicons.Borgs;

/// <summary>
/// Optional sprite overrides for a borg gender variant.
/// </summary>
[DataDefinition]
public sealed partial class BorgGenderSpriteSet
{
    /// <summary>
    /// Main body sprite override.
    /// </summary>
    [DataField]
    public PrototypeLayerData? Body;

    /// <summary>
    /// Movement body sprite override.
    /// </summary>
    [DataField]
    public PrototypeLayerData? BodyMovement;

    /// <summary>
    /// Mind-present indicator override.
    /// </summary>
    [DataField]
    public PrototypeLayerData? HasMind;

    /// <summary>
    /// No-mind indicator override.
    /// </summary>
    [DataField]
    public PrototypeLayerData? NoMind;

    /// <summary>
    /// Active light indicator override.
    /// </summary>
    [DataField]
    public PrototypeLayerData? ToggleLight;
}

/// <summary>
/// Fully resolved borg visual layers after applying gender fallback rules.
/// </summary>
public readonly record struct BorgGenderResolvedVisuals(
    PrototypeLayerData Body,
    PrototypeLayerData? BodyMovement,
    PrototypeLayerData HasMind,
    PrototypeLayerData NoMind,
    PrototypeLayerData ToggleLight);
