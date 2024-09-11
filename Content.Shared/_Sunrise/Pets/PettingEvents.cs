using Content.Shared._Sunrise.Pets.Prototypes;
using Robust.Shared.Prototypes;
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

#endregion

#region Control events

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
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PetSetAILogicEvent : PetBaseEvent
{
    [DataField]
    public PetOrderType Order;

    public PetSetAILogicEvent() {}

    public PetSetAILogicEvent(PetOrderType order)
    {
        Order = order;
    }
}
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
