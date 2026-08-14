using Content.Shared.Chemistry.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseSmokeOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField(required: true)]
    public int SpreadAmount;

    [DataField]
    public EntProtoId SmokePrototype = "Smoke";

    [DataField]
    public Solution Solution = new();
}