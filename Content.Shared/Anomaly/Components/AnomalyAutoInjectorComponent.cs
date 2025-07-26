using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Anomaly.Components;

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
