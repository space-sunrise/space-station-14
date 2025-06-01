using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Clothing.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class EmitsSoundOnMoveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _grid = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<Components.EmitsSoundOnMoveComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<Components.EmitsSoundOnMoveComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, Components.EmitsSoundOnMoveComponent component, GotEquippedEvent args)
    {
        component.IsSlotValid = !args.SlotFlags.HasFlag(SlotFlags.POCKET);
    }

    private void OnUnequipped(EntityUid uid, Components.EmitsSoundOnMoveComponent component, GotUnequippedEvent args)
    {
        component.IsSlotValid = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Components.EmitsSoundOnMoveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var xform = Transform(uid);
            if (!TryComp<PhysicsComponent>(uid, out var physics))
                return;

            if (xform.GridUid == null)
                return;

            if (comp.RequiresGravity && _gravity.IsWeightless(uid, physics, xform))
                return;

            var parent = xform.ParentUid;

            var worn = parent.Valid &&
                       TryComp<ClothingComponent>(uid, out var clothing) &&
                       clothing.InSlot != null &&
                       comp.IsSlotValid;

            var coordinates = worn ? Transform(parent).Coordinates : xform.Coordinates;
            var distanceNeeded = (worn && TryComp<InputMoverComponent>(parent, out var mover) && mover.Sprinting)
                ? 1.5f
                : 2f;

            if (!coordinates.TryDistance(EntityManager, comp.LastPosition, out var distance) ||
                distance > distanceNeeded)
                comp.SoundDistance = distanceNeeded;
            else
                comp.SoundDistance += distance;

            comp.LastPosition = coordinates;
            if (comp.SoundDistance < distanceNeeded)
                return;
            comp.SoundDistance -= distanceNeeded;

            var sound = comp.SoundCollection;
            var audioParams = sound.Params
                .WithVolume(sound.Params.Volume)
                .WithVariation(sound.Params.Variation ?? 0f);

            _audio.PlayPredicted(sound, uid, uid, audioParams);
        }
    }
}
