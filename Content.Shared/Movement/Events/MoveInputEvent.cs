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

    public MoveInputEvent(Entity<InputMoverComponent> entity, MoveButtons oldMovement)
    {
        Entity = entity;
        OldMovement = oldMovement;
    }
}
