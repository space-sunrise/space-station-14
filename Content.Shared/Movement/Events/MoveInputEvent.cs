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

    public bool HasDirectionalMovement => (Entity.Comp.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;

    // Starlight-Abductor-edited

    public readonly Direction Dir;
    public readonly bool State;

    public MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement, Direction dir, bool state) // Starlight-Abductor-edited
    {
        Entity = entity;
        OldMovement = oldMovement;
        Dir = dir;
        State = state;
    }
}
