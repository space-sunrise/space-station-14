using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.DynamicAppearance;

/// <summary>
/// Component that allows entities to edit their appearance in-game through a BUI.
/// Use <see cref="AllowedFields"/> to restrict which categories can be changed,
/// including whether the species itself may be changed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DynamicAppearanceComponent : Component
{
    /// <summary>
    /// Bitmask of appearance fields the player is allowed to edit.
    /// The server enforces this on save and the client hides disabled controls automatically.
    /// Defaults to <see cref="DynamicAppearanceFields.None"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DynamicAppearanceFields AllowedFields = DynamicAppearanceFields.None;

    /// <summary>
    /// Delay before an appearance save is applied.
    /// Admins bypass this entirely.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SaveDelay = TimeSpan.FromSeconds(3);
}
