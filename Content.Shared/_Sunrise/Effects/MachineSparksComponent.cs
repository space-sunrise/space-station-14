using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Effects;

/// <summary>
/// Настраивает эффекты искр для повреждаемых машин.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MachineSparksComponent : Component
{
    /// <summary>
    /// Эффекты, отображаемые при получении машиной урона.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> ImpactEffects = [];

    /// <summary>
    /// Вероятность появления искры при получении урона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ImpactEffectProbability = 0.5f;

    /// <summary>
    /// Эффекты, периодически отображаемые при низкой прочности машины.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> LowHealthEffects = [];

    /// <summary>
    /// Доля порога разрушения, после которой машина считается сильно поврежденной.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowHealthDamageFraction = 0.75f;

    /// <summary>
    /// Минимальная задержка между периодическими эффектами искр.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinLowHealthSparkDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Максимальная задержка между периодическими эффектами искр.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxLowHealthSparkDelay = TimeSpan.FromSeconds(8);
}
