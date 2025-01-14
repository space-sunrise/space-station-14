using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Sunrise.CritHeartbeat;

public sealed class CritHeartbeatSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CritHeartbeatComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<CritHeartbeatComponent> ent, ref MobStateChangedEvent args)
    {
        ent.Comp.AudioStream = args.NewMobState == MobState.Critical
            ? _audio.PlayEntity(ent.Comp.HeartbeatSound, ent, ent, AudioParams.Default.WithLoop(true))?.Entity
            : _audio.Stop(ent.Comp.AudioStream);
    }
}
