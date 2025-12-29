using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.Abilities.Milira;

/// <summary>
/// Компонент, позволяющий раскрывать и складывать крылья путём замены маркингов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class WingToggleComponent : Component
{
    /// <summary>
    /// Прототип экшена, позволяющего переключать состояние крыльев.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionToggleWings";

    /// <summary>
    /// Экземпляр экшена на сущности.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Суффикс, добавляемый к закрытому маркингу для получения открытого варианта.
    /// </summary>
    [DataField]
    public string Suffix = "Open";

    /// <summary>
    /// Индикатор, раскрыты ли сейчас крылья.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool WingsOpened;

    /// <summary>
    /// Тег, который должен быть у предмета одежды, чтобы его можно было одеть при раскрытых крыльях.
    /// Если предмет имеет этот тег, блокировка не сработает.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? AllowedTag = "WingToggleAllowed";

    /// <summary>
    /// Список слотов, которые должны быть свободны для раскрытия крыльев.
    /// </summary>
    [DataField]
    public List<string>? BlockedSlots = new() { "outerClothing" };
}
