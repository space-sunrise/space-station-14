using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Roles;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class OtherJobsTakenRequirementComponent : Component
{
    [DataField("targetJob", customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string TargetJob;

    [DataField("adjustJob", customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string AdjustJob;

    [DataField("coefficient")]
    public int Coefficient;

    public int Accumulator = 0;
}
