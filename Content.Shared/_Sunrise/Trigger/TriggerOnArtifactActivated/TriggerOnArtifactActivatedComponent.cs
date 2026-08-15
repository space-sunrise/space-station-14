using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Trigger.TriggerOnArtifactActivated;

/// <summary>
/// Triggers its entity after a successful xenoartifact activation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnArtifactActivatedComponent : BaseTriggerOnXComponent;
