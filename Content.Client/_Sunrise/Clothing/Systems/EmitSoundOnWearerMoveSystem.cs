using Content.Shared._Sunrise.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Client._Sunrise.Clothing.Systems;

public sealed class EmitSoundOnWearerMoveSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EmitSoundOnWearerMoveComponent, GotEquippedEvent>(OnEquipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmitSoundOnWearerMoveComponent, PhysicsComponent, TransformComponent, ClothingComponent>();
        while (query.MoveNext(out var uid, out var emitSoundOnMoveComponent, out var physics, out var xform, out var clothing))
        {
            if (xform.GridUid == null)
                return;

            if (emitSoundOnMoveComponent.RequiresGravity && _gravity.IsWeightless(uid, physics, xform))
                return;

            var wearer = xform.ParentUid;
            var worn = wearer.Valid &&
                       clothing.InSlot != null &&
                       emitSoundOnMoveComponent.IsValidSlot;

            var coords = worn ? Transform(wearer).Coordinates : xform.Coordinates;
            var dist = (worn && TryComp<InputMoverComponent>(wearer, out var mover) && mover.Sprinting)
                ? 1.5f
                : 2f;
            if (!coords.TryDistance(EntityManager, emitSoundOnMoveComponent.LastPosition, out var distance) ||
                distance > dist)
                emitSoundOnMoveComponent.SoundDistance = dist;
            else
                emitSoundOnMoveComponent.SoundDistance += distance;

            emitSoundOnMoveComponent.LastPosition = coords;
            if (emitSoundOnMoveComponent.SoundDistance < dist)
                return;
            emitSoundOnMoveComponent.SoundDistance -= dist;

            var sound = emitSoundOnMoveComponent.SoundCollection;
            _audio.PlayPredicted(
                sound,
                uid,
                uid,
                sound.Params.WithVolume(sound.Params.Volume).WithVariation(sound.Params.Variation ?? 0f));
        }
    }

    private void OnEquipped(EntityUid uid, EmitSoundOnWearerMoveComponent component, GotEquippedEvent args)
    {
        component.IsValidSlot = !args.SlotFlags.HasFlag(SlotFlags.POCKET);
    }
}
