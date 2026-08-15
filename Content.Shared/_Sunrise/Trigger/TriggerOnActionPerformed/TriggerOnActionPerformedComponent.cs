using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Trigger.TriggerOnActionPerformed;

/// <summary>
/// Raises a trigger after the owning action has been performed successfully.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnActionPerformedComponent : BaseTriggerOnXComponent;
