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
    }

    private void OnMobStateChanged(Entity<CritHeartbeatComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        ent.Comp.AudioStream = args.NewMobState == MobState.Critical
            ? _audio.PlayEntity(ent.Comp.HeartbeatSound, ent, ent, AudioParams.Default.WithLoop(true))?.Entity
            : _audio.Stop(ent.Comp.AudioStream);
    }
}
