using Robust.Shared.GameStates;

namespace Content.Shared.Sunrise.Eye;

/// <summary>
/// Adds overlay for client that darkness vision
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDarkenedVisionSystem))]
public sealed partial class DarkenedVisionComponent : Component
{
    /// <summary>
    /// How much is vision darkened (in tiles from screen edge)
    /// </summary>
    [DataField]
    public float Strength = 0;

    /// <summary>
    /// If strength is higher than this value users goes blind
    /// </summary>
    [DataField]
    public float BlindTreshold = 8;
}
