using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampiricClawsComponent : Component
{
    /// <summary>
    /// How many successful melee hits before the claws dissipate
    /// </summary>
    [DataField, AutoNetworkedField]
    public int HitsRemaining = 15;

    [DataField, AutoNetworkedField]
    public int BloodPerHit = 5;
}
