namespace Content.Server._Sunrise.Clothing.Components;

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
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Break the disguise when the wearer is hit by a melee attack.
    /// </summary>
    [DataField]
    public bool BreakOnAttacked = true;

    /// <summary>
    /// Break the disguise when the wearer performs a melee attack.
    /// </summary>
    [DataField]
    public bool BreakOnMeleeAttack = true;

    /// <summary>
    /// Break the disguise when the wearer fires a gun.
    /// </summary>
    [DataField]
    public bool BreakOnGunShot = true;
}
