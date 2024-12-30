using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Jump;

[NetworkedComponent, RegisterComponent]
public sealed partial class JumpComponent : Component
{
    public Dictionary<string, int> OriginalCollisionMasks { get; } = new();

    public Dictionary<string, int> OriginalCollisionLayers { get; } = new();
};
