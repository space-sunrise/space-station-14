using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Effects;

/// <summary>
/// Конфигурирует sparks impact для повреждаемых машин.
/// </summary>
[RegisterComponent]
public sealed partial class MachineSparksComponent : Component
{
    /// <summary>
    /// Эффекты, которые показываются при попадании по машине.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntProtoId> ImpactEffects = [];

    /// <summary>
    /// Эффекты, которые периодически показываются при малом здоровье машины.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntProtoId> LowHealthEffects = [];

    /// <summary>
    /// Доля урона от порога разрушения, после которой машина считается сильно поврежденной.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float LowHealthDamageFraction = 0.75f;

    /// <summary>
    /// Минимальная задержка между периодическими искрами.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MinLowHealthSparkDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Максимальная задержка между периодическими искрами.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MaxLowHealthSparkDelay = TimeSpan.FromSeconds(8);
}
