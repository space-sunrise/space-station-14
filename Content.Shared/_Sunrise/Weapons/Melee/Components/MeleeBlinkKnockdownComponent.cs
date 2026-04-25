namespace Content.Shared._Sunrise.Weapons.Melee.Components;

[RegisterComponent]
/// <summary>
/// Applies a forced knockdown to entities on the tile where a melee blink finishes.
/// </summary>
public sealed partial class MeleeBlinkKnockdownComponent : Component
{
    /// <summary>
    /// Duration of the knockdown applied to entities occupying the blink landing tile.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan KnockdownDuration;
}
