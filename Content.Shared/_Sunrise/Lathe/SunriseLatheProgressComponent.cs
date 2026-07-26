using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Lathe;

/// <summary>
/// Синхронизирует временной интервал активного производства для клиентской шкалы прогресса.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SunriseLatheProgressComponent : Component
{
    /// <summary>
    /// Время начала производства текущего предмета.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan StartTime;

    /// <summary>
    /// Время окончания производства текущего предмета.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Текущее состояние шкалы производства.
    /// </summary>
    [AutoNetworkedField]
    public SunriseLatheProgressState State;
}

/// <summary>
/// Визуальное состояние шкалы производства.
/// </summary>
[Serializable, NetSerializable]
public enum SunriseLatheProgressState : byte
{
    Running,
    Interrupted,
}
