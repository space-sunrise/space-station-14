using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class CMExplosionEffectComponent : Component
{
    [DataField]
    public EntProtoId? Explosion = "CMExplosionEffectGrenade";

    [DataField]
    public EntProtoId? ShockWave = "RMCExplosionEffectGrenadeShockWave";

    [DataField]
    public EntProtoId? Smoke = "ExplosionEffectSmoke";
}
