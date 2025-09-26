using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Biocode.Components;

/// <summary>
/// Component that automatically deactivates items when they're not in the possession of authorized users.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BiocodeDeactivationComponent : Component
{
    /// <summary>
    /// Whether the item should be deactivated when removed from authorized user's possession.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DeactivateOnRemoval = true;

    /// <summary>
    /// Whether the item should be deactivated when placed in unauthorized user's possession.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DeactivateOnUnauthorized = true;

    /// <summary>
    /// Alert text to show when unauthorized user tries to use the item.
    /// If null, uses the BiocodeComponent's alert text.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? AlertText;
}
