using Content.Shared.Actions;
﻿using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action targeting a position in the world.
/// Requires <see cref="TargetActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WorldTargetActionComponent : Component
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField(required: true), NonSerialized, AutoNetworkedField]
    public WorldTargetActionEvent? Event;
}
