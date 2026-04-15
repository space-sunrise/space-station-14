using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.SiliconStanding;

[RegisterComponent]
public sealed partial class SiliconStandingComponent : Component
{
    [DataField("lieDownDelay")]
    public float LieDownDelay = 1.0f;

    [DataField("standUpDelay")]
    public float StandUpDelay = 0.5f;
}
