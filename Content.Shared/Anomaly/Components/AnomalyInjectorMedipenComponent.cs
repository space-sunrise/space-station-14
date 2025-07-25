using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Anomaly.Components;

namespace Content.Shared.Anomaly.Components;

[RegisterComponent]
public sealed partial class AnomalyInjectorMedipenComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry InjectionComponents = default!;
}

[RegisterComponent]
public sealed partial class UsedAnomalyInjectorMedipenComponent : Component
{
}
