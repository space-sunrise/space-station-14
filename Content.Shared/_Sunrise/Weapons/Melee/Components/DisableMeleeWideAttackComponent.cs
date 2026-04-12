using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.Melee.Components;

/// <summary>
/// Prevents RMB wide melee attacks so the held item can use an alternative in-hand interaction instead.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DisableMeleeWideAttackComponent : Component
{
}
