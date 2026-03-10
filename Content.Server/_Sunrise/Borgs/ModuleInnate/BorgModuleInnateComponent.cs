using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Компонент, позволяющий давать боргам действия (экшены) и компоненты через модуль
/// </summary>
[RegisterComponent]
public sealed partial class BorgModuleInnateComponent : Component
{
    // Прототипы экшенов для встроенных предметов
    // Важно для кастомных экшенов - делайте их Temporary.
    [DataField]
    public EntProtoId InnateUseItemAction = "ModuleInnateUseItemAction";

    [DataField]
    public EntProtoId InnateToggleItemAction = "ModuleInnateToggleItemAction";

    [DataField]
    public EntProtoId InnateInteractionItemAction = "ModuleInnateInteractionItemAction";

    /// <summary>
    /// Множитель потребления энергии предметами модуля
    /// </summary>
    [DataField]
    public float PowerUseCoefficient = 0.5f;

    /// <summary>
    /// Предметы, которые активируются прямо в руке
    /// </summary>
    [DataField]
    public List<EntProtoId?> UseItems = [];

    /// <summary>
    /// Предметы, с помощью которых можно взаимодействовать с сущностями
    /// </summary>
    [DataField]
    public List<EntProtoId?> InteractionItems = [];

    /// <summary>
    /// Предметы, которые переключаются при активации в руке
    /// </summary>
    [DataField]
    public List<EntProtoId?> ToggleItems = [];

    /// <summary>
    /// Компоненты, которые будут добавлены боргу при установке модуля
    /// Будут удалены после его изъятия!
    /// </summary>
    [DataField(customTypeSerializer: typeof(Robust.Shared.Serialization.TypeSerializers.Implementations.ComponentRegistrySerializer))]
    public ComponentRegistry InnateComponents = [];

    /// <summary>
    /// Можно ли заряжать борга предметами из модуля
    /// </summary>
    [DataField]
    public bool CanCharge = false;

    /// <summary>
    /// Контейнер с предметами модуля
    /// </summary>
    [ViewVariables]
    public Container? InnateItemsContainer = null;

    /// <summary>
    /// Экшены для борга, созданные данным модулем
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Actions = [];

    /// <summary>
    /// Список включенных предметов
    /// Данный список нужен сугубо для корректной очистки
    /// </summary>
    [ViewVariables]
    public List<EntityUid> ToggledOn = [];
}
