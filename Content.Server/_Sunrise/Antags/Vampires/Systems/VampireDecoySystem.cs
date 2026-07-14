using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed class VampireDecoySystem : EntitySystem
{
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireDecoyComponent, DamageChangedEvent>(OnDecoyDamaged);
    }

    private void OnDecoyDamaged(Entity<VampireDecoyComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.Detonated || args.DamageDelta is null || !args.DamageDelta.AnyPositive())
            return;

        ent.Comp.Detonated = true;
        Dirty(ent);
        TriggerDecoyFlash(ent);
    }

    private void TriggerDecoyFlash(Entity<VampireDecoyComponent> ent)
    {
        var coords = _transform.GetMapCoordinates(ent);
        var entityCoords = Transform(ent).Coordinates;

        _flash.FlashArea(ent, null, ent.Comp.FlashRange, ent.Comp.FlashDuration, slowTo: ent.Comp.SlowTo,
            displayPopup: ent.Comp.DisplayPopup, probability: ent.Comp.Probability);
        _audio.PlayPvs(ent.Comp.FlashSound, entityCoords, AudioParams.Default.WithVolume(1f).WithMaxDistance(ent.Comp.FlashRange));

        EntityManager.Spawn(ent.Comp.FlashEffectId, coords);
        QueueDel(ent);
    }
}
