using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Mobs;

namespace Content.Server._Sunrise.FleshCult;

public sealed partial class FleshCultSystem
{
    public void InitializeMob()
    {
        SubscribeLocalEvent<FleshMobComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, FleshMobComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;
        if (component.SoundDeath == null)
            return;
        _audioSystem.PlayPvs(component.SoundDeath, uid, component.SoundDeath.Params);

        var coords = _transformSystem.GetMapCoordinates(uid);

        for (var i = 0; i < component.DeathMobSpawnCount; i++)
        {
            Spawn(component.DeathMobSpawnId, coords);
        }
    }
}

