using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Flip
{
    [NetworkedComponent, RegisterComponent]
    public sealed partial class FlipComponent : Component
    {
        public Dictionary<string, int> OriginalCollisionLayers { get; } = new();
    }
}
