using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;

/// <summary>
/// Makes a spawned decoy copy visual data from the owning vampire.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class VampireDecoyAppearanceComponent : Component
{
    /// <summary>
    /// The entity that should be visually duplicated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Source;
}
