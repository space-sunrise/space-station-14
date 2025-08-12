using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Nesting;

[RegisterComponent, NetworkedComponent]
public sealed partial class NestingMobComponent : Component
{
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(5.0);

    [ViewVariables]
    public bool InContainer;
}
