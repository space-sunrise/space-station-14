using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Roles;

/// <summary>
/// Компонент, вещающийся на станцию, чтобы увеличивать слоты на работы в зависимости различных условий.
/// Увеличение происходит как только игрок заходит за роль.
/// </summary>
[RegisterComponent]
public sealed partial class RelativeJobsCountComponent : Component
{
    /// <summary>
    /// Список параметров для ролей, которые будут увеличивать количество слотов одной роли за счет игроков на другой роли.
    /// </summary>
    [DataField]
    public HashSet<JobRelativeCount> Jobs = [];

    /// <summary>
    /// Список параметров для ролей, которые будут увеличивать количество слотов одной роли за общего онлайна сервера.
    /// </summary>
    [DataField]
    public HashSet<OnlineRelativeCount> Online = [];

    /// <summary>
    /// Словарь, хранящий максимальное количество слотов для каждой из работ.
    /// Значения тут будут являться крайней точкой, которая ограничивает количество слотов.
    /// Это существует, чтобы отдельно разные условия(от количества ролей и от онлайна) не создавали слишком много неконтроллируемых слотов.
    /// Ключ -> работа, слоты которой будут ограничены.
    /// Значение -> Количество максимальных слотов. Без учета того, что написано в перечислении ролей у карты. Поставьте -1 чтоб выключить ограничение сверху
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, int> TotalMaxCount;
}

/// <summary>
/// Структура используемая как параметр настройки зависимости количества слотов определенной роли от количества игроков на других ролях.
/// </summary>
[DataDefinition]
public partial record struct JobRelativeCount : IRelativeCountSettings
{
    /// <summary>
    /// Работа, на которую будут выдаваться слоты.
    /// </summary>
    [DataField(required: true)] public ProtoId<JobPrototype> TargetJob { get; set; }

    /// <summary>
    /// Словарь с зависимостями.
    /// Ключ - работа, игроки на которой будут выдавать доп слоты для TargetJob.
    /// Значение - количество открываемых слотов за каждого игрока на переданной в ключе работе.
    /// </summary>
    [DataField(required: true)] public Dictionary<ProtoId<JobPrototype>, int> Dependency { get; set; }

    /// <summary>
    /// Максимум слотов, которые будут открыты для данной роли через количество других ролей.
    /// </summary>
    [DataField] public int MaxSlots { get; set; } = -1;
}

/// <summary>
/// Структура используемая как параметр настройки зависимости количества слотов определенной роли от общего онлайна на сервере.
/// </summary>
[DataDefinition]
public partial record struct OnlineRelativeCount : IRelativeCountSettings
{
    /// <summary>
    /// Работа, на которую будут выдаваться слоты.
    /// </summary>
    [DataField(required: true)] public ProtoId<JobPrototype> TargetJob { get; set; }

    /// <summary>
    /// Сколько игроков онлайна будут выдавать дополнительный слот?
    /// Пример: 20 = каждые 20 игроков открывается новый слот.
    /// </summary>
    [DataField(required: true)] public float AnyTargetOnlineIncreaseSlot { get; set; }

    /// <summary>
    /// Максимум слотов, которые будут открыты для данной роли через количество онлайна.
    /// </summary>
    [DataField] public int MaxSlots { get; set; } = -1;
}

/// <summary>
/// Общий-интерфейс шаблон для построения зависимости количества слотов работы от чего-то.
/// Включает обязательные и общие для всех параметры, которые обязаны быть использованы.
/// </summary>
public interface IRelativeCountSettings
{
    /// <summary>
    /// Работа, на которую будут выдаваться слоты.
    /// </summary>
   public ProtoId<JobPrototype> TargetJob { get; set; }

    /// <summary>
    /// Максимум слотов, которые будут открыты для данной роли через данные настройки.
    /// </summary>
    /// TODO: Проверить, что это вообще работает, если отличается от максимального количества из компонента
    public int MaxSlots { get; set; }
}
