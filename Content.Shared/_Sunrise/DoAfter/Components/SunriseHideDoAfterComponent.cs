using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.DoAfter.Components;

/// <summary>
/// Маркер, запрещающий отображать do-after исполнителя другим игрокам.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SunriseHideDoAfterComponent : Component;
