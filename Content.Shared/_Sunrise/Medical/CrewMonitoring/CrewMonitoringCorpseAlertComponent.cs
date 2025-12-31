using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.CrewMonitoring;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrewMonitoringCorpseAlertComponent : Component
{
    // падшие с сенсорами... Это люди, у которых датчики, и они мертвы или в крите.

    /// <summary>
    ///     Включено ли оповещение о падших с сенсорами.
    /// </summary>
    [DataField]
    public bool DoCorpseAlert = false;

    /// <summary>
    ///     Следующее время, когда можно проигрывать оповещение о падших с сенсорами.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCorpseAlertTime = TimeSpan.Zero;

    /// <summary>
    ///     Время между проверками на наличие падших с сенсорами вне моргов.
    /// </summary>
    [DataField]
    public float CorpseAlertTime = 15f;

    /// <summary>
    ///     Звук оповещения о падших с сенсорами.
    /// </summary>
    [DataField]
    public SoundSpecifier CorpseAlertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
}
