using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.CritHeartbeat;

public sealed class CritHeartbeatSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CritHeartbeatComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CritHeartbeatComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnMobStateChanged(Entity<CritHeartbeatComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        ent.Comp.AudioStream = args.NewMobState == MobState.Critical
            ? _audio.PlayEntity(ent.Comp.HeartbeatSound, ent, ent)?.Entity
            : _audio.Stop(ent.Comp.AudioStream);
    }

    private void OnDamage(Entity<CritHeartbeatComponent> ent, ref DamageChangedEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (!Exists(ent.Comp.AudioStream))
            return;

        var pitch = Math.Min(1, 100 / args.Damageable.TotalDamage.Float());

        // Потому что игра говно, тут нельзя изменять аудиопарамс уже существующего звука. Поэтому я пересоздаю его заново
        // Это приводит к проигрыванию звука через неравномерные промежутки времени, но зато работает и не очень заметно
        _audio.Stop(ent.Comp.AudioStream);
        ent.Comp.AudioStream = _audio.PlayEntity(ent.Comp.HeartbeatSound, ent, ent, AudioParams.Default.WithPitchScale(pitch))?.Entity;
    }
}
