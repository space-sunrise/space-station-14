using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Abilities.Resomi;

/// <summary>
/// Маркер: сущность находится в активной фазе способности (резоми-ры́вок и т.п.).
/// Используется для запрета падения/лежа во время исполнения.
/// </summary>
/// <remarks>
/// Добавляется/снимается на сервере ResomiSkillSystem; читается в SharedStandingStateSystem.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResomiActiveAbilityComponent  : Component {}
