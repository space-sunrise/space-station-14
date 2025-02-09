using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity whenever it has a movement input change.
/// </summary>
[ByRefEvent]
public readonly struct MoveInputEvent
{
    public readonly Entity<InputMoverComponent> Entity;
    public readonly MoveButtons OldMovement;
    // Sunrise-start
    public readonly Direction Dir;
    public readonly bool State;
    // Sunrise-end

    public bool HasDirectionalMovement => (Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;

    public MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement, Direction dir, bool state) // Sunrise-edit
    {
        Entity = entity;
        OldMovement = oldMovement;
        // Sunrise-start
        Dir = dir;
        State = state;
        // Sunrise-end
    }
}
