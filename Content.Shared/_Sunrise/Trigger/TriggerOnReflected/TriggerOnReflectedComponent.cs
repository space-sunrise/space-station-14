using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Trigger.TriggerOnReflected;

/// <summary>
/// Triggers its entity after it reflects a projectile or hitscan attack.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnReflectedComponent : BaseTriggerOnXComponent;
