using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Misc;

/// <summary>
/// Компонент маркер, что данная цель смешная.
/// Используется в <see cref="ArtifactWhitelistSwapSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArtifactFunnyTargetComponent : Component;
