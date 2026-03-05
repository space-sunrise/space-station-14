using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Sunrise.NPC;

[RegisterComponent]
[ComponentProtoName("PirateVeteranFollower")]
public sealed partial class PirateVeteranFollowerComponent : Component
{
    [DataField]
    public float SearchRadius = 35f;

    [DataField]
    public float RecheckCooldown = 30f;

    [DataField]
    public float RecheckAccumulator;
}

