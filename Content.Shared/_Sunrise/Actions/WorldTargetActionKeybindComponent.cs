using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Actions;

/// <summary>
/// Allows an entity to perform a configured world-target action through a pointer keybind.
/// </summary>
[RegisterComponent]
public sealed partial class WorldTargetActionKeybindComponent : Component
{
    /// <summary>
    /// Action prototype to execute with the configured pointer keybind.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action = string.Empty;
}
