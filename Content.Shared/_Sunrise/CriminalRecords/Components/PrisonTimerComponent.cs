using Content.Shared.Access.Components;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CriminalRecords.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrisonTimerComponent : Component
{
    /// <summary>
    ///     If true, manual editing via UI/interaction is blocked.
    /// </summary>
    [DataField]
    public bool Locked = false;
}
