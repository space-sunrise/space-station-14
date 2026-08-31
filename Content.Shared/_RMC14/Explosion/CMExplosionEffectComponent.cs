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

    // Sunrise added
    [DataField]
    public EntProtoId? Smoke = "ExplosionEffectSmoke";
    // Sunrise added
}

// Sunrise added start
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionSmokeEffectComponent : Component
{
    public const float AnimationDuration = 2.5f;
    public const float Variation = 1f;

    [DataField, AutoNetworkedField]
    public float LifeTime = AnimationDuration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionDustEffectComponent : Component
{
    /// <summary>
    /// Стандартная длительность движения пыли.
    /// </summary>
    public const float AnimationDuration = 5f;

    /// <summary>
    /// Длительность смещения пыли до удаления сущности.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LifeTime = AnimationDuration;

    /// <summary>
    /// Минимальная дистанция визуального смещения пыли.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinDriftDistance = 0.25f;

    /// <summary>
    /// Максимальная дистанция визуального смещения пыли.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDriftDistance = 0.5f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionEffectComponent : Component
{
    public const float AnimationDuration = 2.5f;

    [DataField, AutoNetworkedField]
    public float LifeTime = AnimationDuration;

    [DataField, AutoNetworkedField]
    public float SizeModifier = 2f;
}
// Sunrise added end
