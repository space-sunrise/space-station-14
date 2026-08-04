using Content.Shared._Sunrise.CollectiveMind;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CollectiveMind;

[RegisterComponent, Access(typeof(CollectiveMindSystem))]
public sealed partial class CollectiveMindGroupComponent : Component
{
    /// <summary>
    /// Тип коллективного разума, к которому относится группа.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CollectiveMindPrototype> Mind;

    /// <summary>
    /// Владелец группы. Членство и право на отправку сообщений в рамках группы определяется владельцем.
    /// Если нет владельца -> группа считается общей.
    /// </summary>
    [ViewVariables]
    public EntityUid? GroupOwner;
}
