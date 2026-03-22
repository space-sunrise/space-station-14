namespace Content.Shared._Sunrise.Clothing.Components;

/// <summary>
/// Makes disguise clothing automatically deactivate when the wearer is revealed by combat actions.
/// </summary>
[RegisterComponent]
public sealed partial class BreakDisguiseOnActionComponent : Component
{
    /// <summary>
    /// How long the disguise stays unavailable after being forcibly broken.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.Zero;

    /// <summary>
    /// Popup shown when the wearer tries to reactivate the disguise during cooldown.
    /// </summary>
    [DataField]
    public LocId CooldownPopup = "break-disguise-on-action-cooldown";
}
