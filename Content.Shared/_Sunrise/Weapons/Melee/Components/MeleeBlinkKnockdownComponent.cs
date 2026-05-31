namespace Content.Shared._Sunrise.Weapons.Melee.Components;

/// <summary>
/// Applies a forced knockdown to one entity near the coordinates where a melee blink finishes.
/// </summary>
[RegisterComponent]
public sealed partial class MeleeBlinkKnockdownComponent : Component
{
    /// <summary>
    /// Duration of the knockdown applied to entities occupying the blink landing tile.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan KnockdownDuration;

    /// <summary>
    /// Radius around the blink landing coordinates to search for a knockdown target.
    /// </summary>
    [DataField]
    public float Radius = 1.25f;
}
