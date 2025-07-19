using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Carrying
{
    /// <summary>
    /// Stores the carrier of an entity being carried.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BeingCarriedComponent : Component
    {
        public EntityUid Carrier = default!;
    }
}
