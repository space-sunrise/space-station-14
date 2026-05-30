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
        if (ent.Comp.Detonated || args.DamageDelta == null || !args.DamageDelta.AnyPositive())
            return;

        ent.Comp.Detonated = true;
        Dirty(ent);
        TriggerDecoyFlash(ent);
    }

    private void TriggerDecoyFlash(Entity<VampireDecoyComponent> ent)
    {
        var (uid, comp) = ent;
        var coords = _transform.GetMapCoordinates(uid);
        var entityCoords = Transform(uid).Coordinates;

        _flash.FlashArea(uid, null, comp.FlashRange, comp.FlashDuration, slowTo: comp.SlowTo, displayPopup: comp.DisplayPopup, probability: comp.Probability);
        _audio.PlayPvs(comp.FlashSound, entityCoords, AudioParams.Default.WithVolume(1f).WithMaxDistance(comp.FlashRange));

        EntityManager.SpawnEntity(comp.FlashEffectId, coords);
        QueueDel(uid);
    }
}
