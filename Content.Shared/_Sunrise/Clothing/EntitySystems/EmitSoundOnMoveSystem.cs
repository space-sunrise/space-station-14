using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Content.Shared._Sunrise.Clothing.Components;

namespace Content.Shared._Sunrise.Clothing.EntitySystems;

public sealed class EmitSoundOnMoveSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EmitSoundOnMoveComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EmitSoundOnMoveComponent, GotUnequippedEvent>(OnUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmitSoundOnMoveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var xform = Transform(uid);
            if (!TryComp<PhysicsComponent>(uid, out var physics))
                return;

            if (xform.GridUid == null)
                return;

            if (comp.RequiresGravity && _gravity.IsWeightless(uid, physics, xform))
                return;

            var wearer = xform.ParentUid;
            var worn = wearer.Valid &&
                       TryComp<ClothingComponent>(uid, out var clothing) &&
                       clothing.InSlot != null &&
                       comp.IsValidSlot;

            var coords = worn ? Transform(wearer).Coordinates : xform.Coordinates;
            var dist = (worn && TryComp<InputMoverComponent>(wearer, out var mover) && mover.Sprinting)
                ? 1.5f
                : 2f;
            if (!coords.TryDistance(EntityManager, comp.LastPosition, out var distance) ||
                distance > dist)
                comp.SoundDistance = dist;
            else
                comp.SoundDistance += distance;

            comp.LastPosition = coords;
            if (comp.SoundDistance < dist)
                return;
            comp.SoundDistance -= dist;

            var sound = comp.SoundCollection;
            _audio.PlayPredicted(
                sound,
                uid,
                uid,
                sound.Params.WithVolume(sound.Params.Volume).WithVariation(sound.Params.Variation ?? 0f));
        }
    }

    private void OnEquipped(EntityUid uid, Components.EmitSoundOnMoveComponent component, GotEquippedEvent args)
    {
        component.IsValidSlot = !args.SlotFlags.HasFlag(SlotFlags.POCKET);
    }

    private void OnUnequipped(EntityUid uid, Components.EmitSoundOnMoveComponent component, GotUnequippedEvent args)
    {
        component.IsValidSlot = true;
    }
}
