using Robust.Shared.GameStates;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared._Sunrise.Trigger;

/// <summary>
/// Removes ensnares from the entity.
/// If TargetUser is true the user will be unsnared instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnsnareOnTriggerComponent : BaseXOnTriggerComponent
{
}
