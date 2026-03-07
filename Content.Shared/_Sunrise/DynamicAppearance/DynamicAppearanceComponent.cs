using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.DynamicAppearance;

/// <summary>
/// Component that allows entities to edit their appearance in-game through a BUI.
/// Use <see cref="AllowedFields"/> to restrict which categories can be changed.
/// </summary>
[RegisterComponent]
public sealed partial class DynamicAppearanceComponent : Component
{
    /// <summary>
    /// Bitmask of appearance fields the player is allowed to edit.
    /// The server enforces this on save and the client hides disabled controls automatically.
    /// Defaults to <see cref="DynamicAppearanceFields.All"/>.
    /// </summary>
    [DataField]
    public DynamicAppearanceFields AllowedFields = DynamicAppearanceFields.All;
}
