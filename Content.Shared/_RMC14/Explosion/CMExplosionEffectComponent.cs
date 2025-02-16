using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class CMExplosionEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? Explosion = "CMExplosionEffectGrenade";

    [DataField, AutoNetworkedField]
    public EntProtoId? ShockWave = "RMCExplosionEffectGrenadeShockWave";
}
