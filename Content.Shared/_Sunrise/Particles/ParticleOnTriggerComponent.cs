using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Starts one or more finite particle orchestras when the entity receives a matching trigger.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParticleOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>Particle orchestras started by the trigger.</summary>
    [DataField(required: true), AutoNetworkedField]
    public List<ParticleOrchestraSpecifier> Orchestras = [];
}
