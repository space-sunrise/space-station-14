using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Pets;

#region System events

/// <summary>
/// Вызывается, когда у питомца меняется владелец
/// </summary>
public sealed class PetMasterChanged: HandledEntityEventArgs
{
    /// <summary>
    /// Новый владелец питомца
    /// </summary>
    public EntityUid NewMaster { get; set; }
}

/// <summary>
/// Вызывается, когда питомец спавнится от лоадаута.
/// Нужен, чтобы автоматически привязывать выбращего данный лоадаут человека к питомцу.
/// Виздены не придумали нормального способа отследить получение лоадаута.
/// </summary>
public sealed class LoadoutPetSpawned : EntityEventArgs
{
    /// <summary>
    /// Игрок, выбравший лоадаут с питомцем.
    /// Питомец будет автоматически привязан к этому EntityUid.
    /// </summary>
    public EntityUid Master { get; set; }

    public LoadoutPetSpawned(EntityUid master)
    {
        Master = master;
    }
}

#endregion

#region Control events
/// <summary>
/// Базовый ивент, используемый для управления питомцем.
/// Содержит поле Entity, которое должно содержать EntityUid самого питомца.
/// Это нужно, так как ивенты отправляются как NetworkedEvent и не содержат фильтрации по компонентам
/// </summary>
[Serializable, NetSerializable]
public abstract class PetBaseEvent : EntityEventArgs
{
    public NetEntity Entity;

    public PetBaseEvent() {}

    public PetBaseEvent(NetEntity entity)
    {
        Entity = entity;
    }
}

/// <summary>
/// Ивент, вызываемый при двух случаях:
/// 1. При нажатии на кнопку в категории смены поведения питомца
/// 2. При приручении
/// Сообщает о смене приказа для ИИ питомца.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PetSetAILogicEvent : PetBaseEvent
{
    [DataField]
    public PetOrderType Order;

    [NonSerialized]
    public EntityUid? Target;

    public PetSetAILogicEvent() {}

    public PetSetAILogicEvent(PetOrderType order, EntityUid? target = null)
    {
        Order = order;
        Target = target;
    }
}

/// <summary>
/// Ивент, вызываемый при нажатии на кнопку в управлении питомцем.
/// Сообщает о переключении возможности призрака вселиться в игрока.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PetSetGhostAvaliable : PetBaseEvent
{
    [DataField]
    public bool Enable;

    public PetSetGhostAvaliable() {}

    public PetSetGhostAvaliable(bool enable)
    {
        Enable = enable;
    }
}

/// <summary>
/// Ивент, вызываемый при нажатии кнопки переименования питомца в меню управления питомца.
/// Сообщает, что питомцу требутеся сменить имя на переданное в ивенте.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PetSetName : PetBaseEvent
{
    public string Name = default!;

    public PetSetName() {}

    public PetSetName(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Ивент вызываемый при использоваии акшена у хозяина для открытия меню всех питомцев.
/// </summary>
public sealed partial class PetOpenAllUiEvent : InstantActionEvent {}


/// <summary>
/// Ивент вызываемый при использоваии акшена у хозяина.
/// Выбирает цель для атаки питомцев
/// </summary>
public sealed partial class PetAttackTargetEvent : EntityTargetActionEvent {}
#endregion

#region Enums

/// <summary>
/// Требуется для определения интерфейса
/// </summary>
[Serializable, NetSerializable]
public enum PetControlUiKey : byte
{
    Key
}

#endregion
