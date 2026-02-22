// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.TimeWindow;
using Robust.Shared.GameStates;

namespace Content.Shared._Nox.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseMutationComponent : Component
{
    /// <summary>
    ///     Дополнительный шанс мутации.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float AddMutationChance = 0.1f;

    /// <summary>
    ///     Может ли существо очистить сущность от вируса.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool CanClear = false;

    /// <summary>
    ///     Нужно ли менять отображение сущности?
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool ChangeApperance = false;

    /// <summary>
    ///     Окно времени обновления мутации.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow UpdateWindow = new TimedWindow(TimeSpan.FromSeconds(3f), TimeSpan.FromSeconds(60f));

    #region Visualizer

    [DataField]
    public string State = "icon";

    [DataField]
    public string InfectedState = "infected";

    #endregion
}
