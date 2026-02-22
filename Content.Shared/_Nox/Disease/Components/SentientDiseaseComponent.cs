// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.TimeWindow;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Nox.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SentientDiseaseComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId ShopMutationAbility = "ShopMutationAbility";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ShopMutationActionEntity;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId SelectPrimaryPatientAbility = "SelectPrimaryPatientAbility";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SelectPrimaryPatientActionEntity;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId TeleportToPrimaryPatientAbility = "TeleportToPrimaryPatientAbility";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? TeleportToPrimaryPatientActionEntity;

    /// <summary>
    ///     Итерация выбранного первичного заражённого для телепорта.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int SelectedPrimaryInfected = 0;

    /// <summary>
    ///     Максимальное количество первичных заражённых.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int MaxPrimaryInfected = 3;

    /// <summary>
    ///     Текущие первичные заражённые.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> CurrentPrimaryInfected = new();

    /// <summary>
    ///     Сколько всего было первичных заражённых.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int FactPrimaryInfected = 0;

    /// <summary>
    ///     Окно времени обновления.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow UpdateWindow = new TimedWindow(TimeSpan.FromSeconds(2f), TimeSpan.FromSeconds(2f));

    /// <summary>
    ///     Данные об вирусе.
    /// </summary>
    [DataField]
    public DiseaseData? Data = null;
}
