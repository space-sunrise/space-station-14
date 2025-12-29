using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Jump;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanJumpComponent : Component
{
    [DataField]
    public TimeSpan JumpInAirTime = TimeSpan.FromMilliseconds(500);

    [DataField]
    public Dictionary<string, int> OriginalCollisionMasks = new();

    [DataField]
    public Dictionary<string, int> OriginalCollisionLayers = new();

    [DataField]
    public bool IsOnlyEmotion = true;

    /// <summary>
    /// In percent
    /// </summary>
    [DataField]
    public float StaminaDamage = 0.1f;

    /// <summary>
    /// In percent
    /// </summary>
    [DataField]
    public float MinimumStamina = 0.5f;
};
