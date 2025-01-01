using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Flesh;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.FleshCult
{
    public sealed class FleshMobSystem : SharedFleshMobSystem
    {
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

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
}

