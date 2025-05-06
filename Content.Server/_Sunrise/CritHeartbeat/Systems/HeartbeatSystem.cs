using Content.Server._Sunrise.CritHeartbeat.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.CritHeartbeat.Systems;

public sealed partial class HeartbeatSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/heartbeat.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CritHeartbeatComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ActiveHeartbeatComponent, DamageChangedEvent>(OnDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveHeartbeatComponent>();

        while (query.MoveNext(out var uid, out var activeHeartbeat))
        {
            if (_timing.CurTime < activeHeartbeat.NextHeartbeatTime)
                continue;

            _audio.PlayGlobal(HeartbeatSound, uid, AudioParams.Default.WithPitchScale(activeHeartbeat.Pitch));

            SetNextTime(activeHeartbeat);
        }
    }

    /// <summary>
    /// Устанавливает время следующего удара сердца
    /// </summary>
    private void SetNextTime(ActiveHeartbeatComponent component)
    {
        component.NextHeartbeatTime = _timing.CurTime + component.NextHeartbeatCooldown;
    }
}
