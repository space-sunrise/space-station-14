using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Carrying
{
    /// <summary>
    /// Added to an entity when they are carrying somebody.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CarryingComponent : Component
    {
        [DataField]
        public EntityUid Carried = default!;
    }
}
