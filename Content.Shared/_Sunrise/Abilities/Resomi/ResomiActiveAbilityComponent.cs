using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Abilities.Resomi;

/// <summary>
/// Marker component used while the Resomi jump ability is active to block certain interactions (e.g. falling).
/// </summary>
/// <remarks>
/// Added and removed by <see cref="Content.Server._Sunrise.Abilities.Jump.JumpSkillSystem"/> on the server; consumed by <see cref="SharedResomiAbilitySystem"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResomiActiveAbilityComponent : Component {}
