using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HemomancerComponent : Component
{
    [AutoNetworkedField]
    public bool InSanguinePool;

    [AutoNetworkedField]
    public bool BloodBringersRiteActive;
}
