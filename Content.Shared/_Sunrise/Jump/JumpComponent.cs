using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Jump;

[RegisterComponent, NetworkedComponent]
public sealed partial class JumpComponent : Component
{
    public static readonly TimeSpan JumpInAirTime = TimeSpan.FromMilliseconds(500);

    public Dictionary<string, int> OriginalCollisionMasks { get; } = new();

    public Dictionary<string, int> OriginalCollisionLayers { get; } = new();
};
