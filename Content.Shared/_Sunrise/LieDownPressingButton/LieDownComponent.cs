using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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


[Serializable, NetSerializable]
public sealed partial class LieDownDoAfterEvent : SimpleDoAfterEvent
{
}
