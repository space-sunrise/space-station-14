using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Disease.Components;

/// <summary>
///     Маркерный компонент для отображения заражений в режиме песочницы.
///     Добавляется/удаляется через addcomp/rmcomp консольными командами.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseSandboxVisibilityComponent : Component;
