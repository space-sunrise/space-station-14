using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Gavel;

public sealed class GavelSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GavelHammerComponent, AfterInteractEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, GavelHammerComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp<GavelBlockComponent>(args.Target, out var comp))
            return;

        if ((comp.PrevSound != null && _timing.CurTime - comp.PrevSound > comp.Cooldown) || comp.PrevSound == null)
        {
            _audio.PlayPvs(comp.HitSound, args.Target.Value);
            comp.PrevSound = _timing.CurTime;
        }
    }
}
