using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.Abilities.Milira;

/// <summary>
/// Компонент, позволяющий раскрывать и складывать крылья путём замены маркингов.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WingToggleComponent : Component
{
    /// <summary>
    /// Прототип экшена, позволяющего переключать состояние крыльев.
    /// </summary>
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
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool WingsOpened;
}
