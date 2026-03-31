namespace Content.Server._Sunrise.Fun.DieOfFate;

[RegisterComponent]
public sealed partial class DieOfFateComponent : Component
{
    /// <summary>
    /// Tracks how many times each player has rolled this die.
    /// Key is the player's EntityUid, value is the number of rolls.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, int> RollsByPlayer = new();

    /// <summary>
    /// Maximum number of times each player can roll this die.
    /// -1 means unlimited.
    /// </summary>
    [DataField]
    public int MaxUsesPerPlayer = 1;
}
