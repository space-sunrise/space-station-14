using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Antags.Vampires.Components;

/// <summary>
/// Состояние вампира.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class VampireComponent : Component
{
    /// <summary>
    /// Всего крови гуманоидов.
    /// </summary>
    public int TotalBlood;

    /// <summary>
    /// Запас крови.
    /// </summary>
    [AutoNetworkedField]
    public int DrunkBlood;

    /// <summary>
    /// Клыки выпущены.
    /// </summary>
    [AutoNetworkedField]
    public bool FangsExtended;

    /// <summary>
    /// Текущая сытость.
    /// </summary>
    [AutoNetworkedField]
    public float BloodFullness = 90f;

    /// <summary>
    /// Предел сытости.
    /// </summary>
    [AutoNetworkedField]
    public float MaxBloodFullness = 200f;

    /// <summary>
    /// Убывание сытости в секунду.
    /// </summary>
    public float FullnessDecayPerSecond = 0.15f;

    /// <summary>
    /// Скорость ходьбы при голоде.
    /// </summary>
    [DataField]
    public float StarvationWalkSpeedModifier = 0.7f;

    /// <summary>
    /// Скорость бега при голоде.
    /// </summary>
    [DataField]
    public float StarvationSprintSpeedModifier = 0.7f;

    /// <summary>
    /// Расход крови при голоде.
    /// </summary>
    [DataField]
    public int StarvationDrunkBloodDrainPerSecond = 2;

    /// <summary>
    /// Остаток расхода крови.
    /// </summary>
    public float StarvationDrunkBloodDrainAccumulator;

    /// <summary>
    /// Текущий уровень силы.
    /// </summary>
    [DataField, AutoNetworkedField]
    public VampirePowerLevel PowerLevel = VampirePowerLevel.Neonate;

    /// <summary>
    /// Следующее обновление.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Предыдущее обновление.
    /// </summary>
    [AutoPausedField]
    public TimeSpan LastUpdate;

    /// <summary>
    /// Интервал обновления.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Слои счётчика крови.
/// </summary>
public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
    Digit4,
}
