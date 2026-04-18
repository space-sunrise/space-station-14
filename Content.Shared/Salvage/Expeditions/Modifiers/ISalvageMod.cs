namespace Content.Shared.Salvage.Expeditions.Modifiers;

public partial interface ISalvageMod // Sunrise - edit 
{
    /// <summary>
    /// Player-friendly version describing this modifier.
    /// </summary>
    LocId Description { get; }

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    float Cost { get; }

}
