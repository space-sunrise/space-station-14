using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Tracker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(TrackerSystem))]
public sealed partial class TrackerComponent : Component
{
    // UpdateAt: Время следующего обновления трекера
    // - Использует кастомный сериализатор для корректной работы со временем
    // - AutoPausedField автоматически приостанавливает таймер при паузе игры
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateAt;

    // UpdateEvery: Интервал между обновлениями трекера
    // - Определяет как часто система будет проверять и обновлять цель отслеживания
    // - По умолчанию 1 секунда
    // - Синхронизируется по сети (AutoNetworkedField)
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    // TrackedComponents: Набор компонентов, которые должны отслеживаться
    // - Содержит имена компонентов (строки), которые система будет искать у других сущностей
    // - Пример: {"TransformComponent", "HealthComponent"}
    // - Синхронизируется по сети
    [DataField, AutoNetworkedField]
    public HashSet<string> TrackedComponents = new();

    // Target: Текущая цель отслеживания
    // - EntityUid сущности, за которой в данный момент ведется слежение
    // - Если null, система пытается найти подходящую цель автоматически
    // - Синхронизируется по сети
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    // Alert: Идентификатор прототипа алерта для отображения
    // - Определяет какой alert-прототип будет использоваться для отображения направления
    // - По умолчанию "TrackerAlert" (должен быть определен в alerts.yml)
    // - Использует ProtoId для типобезопасной работы с прототипами
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "TrackerAlert";
}
