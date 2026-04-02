using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Silicons.Bots.Components;

[RegisterComponent]
public sealed partial class CleanBotDetainOnHitComponent : Component
{
    [DataField]
    public EntProtoId HandcuffPrototype = "Zipties";

    [DataField]
    public float Duration = 1.5f;

    [ViewVariables]
    public bool Enabled = true;

    [ViewVariables]
    public bool IsDetaining;

    [ViewVariables]
    public TimeSpan DetainEndTime;
}
