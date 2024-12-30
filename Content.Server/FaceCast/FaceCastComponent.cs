
using Content.Shared.FixedPoint;

namespace Content.Server.FaceCast;

[RegisterComponent]
public sealed partial class FaceCastComponent : Component
{
    public TimeSpan? StartCastingTime = null;

    public EntityUid? Equiper = null;

    public Double TimeToCast = 5;
}
