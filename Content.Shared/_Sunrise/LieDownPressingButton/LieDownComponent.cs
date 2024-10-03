using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.SharedLieDownPressingButtonSystem
{
    // Mark for entitites which were proccessed by system so it shouldn't be used without system
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class LieDownComponent : Component
    {
        [AutoNetworkedField]
        public bool Lied { get; set; } = false;
    }
}
