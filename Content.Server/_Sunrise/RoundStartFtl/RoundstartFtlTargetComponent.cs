using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.RoundStartFtl;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class RoundstartFtlTargetComponent : Component
{
    [DataField]
    public ResPath? GridPath { get; set; }
}
