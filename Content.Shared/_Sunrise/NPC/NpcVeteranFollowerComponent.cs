using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Sunrise.NPC;

[RegisterComponent]
public sealed partial class NpcVeteranFollowerComponent : Component
{
    [DataField]
    public string BossTag = "NpcBoss";

    [DataField]
    public string VeteranLeaderTag = "NpcVeteranLeader";

    [DataField]
    public float SearchRadius = 25f;

    [DataField]
    public float RecheckCooldown = 30f;

    [DataField]
    public float RecheckAccumulator;
}

