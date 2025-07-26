using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Components;

[RegisterComponent]
public sealed partial class AnomalyAutoInjectorComponent : Component
{
    // InjectionComponents поле удалено
}

[RegisterComponent, NetworkedComponent]
public sealed partial class UsedAnomalyAutoInjectorComponent : Component
{
}
