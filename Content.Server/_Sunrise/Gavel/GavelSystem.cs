﻿using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Gavel;

public sealed class GavelSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GavelHammerComponent, AfterInteractEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, GavelHammerComponent component, AfterInteractEvent args)
    {
        if (args.Target == null ||
            args.CanReach == false ||
            args.Handled ||
            !TryComp<GavelBlockComponent>(args.Target, out var comp))
            return;

        if ((comp.PrevSound != null && _timing.CurTime - comp.PrevSound > comp.Cooldown) || comp.PrevSound == null)
        {
            comp.Counter += 1;
            _audio.PlayPvs(comp.HitSound, args.Target.Value);
            comp.PrevSound = _timing.CurTime;
        }

        // Дабы не спамили, звук ведь громкий и может на нервы давить.
        if (comp.Counter > comp.MaxCounter)
        {
            var blockMeta = MetaData(args.Target.Value);
            var hammerMeta = MetaData(uid);

            _popup.PopupEntity(Loc.GetString("gavel-broken", ("ent", blockMeta.EntityName)), args.Target.Value, PopupType.MediumCaution);
            _popup.PopupEntity(Loc.GetString("gavel-broken", ("ent", hammerMeta.EntityName)), uid, PopupType.MediumCaution);

            _metaData.SetEntityName(args.Target.Value, $"{Loc.GetString("gavel-broken-entityname-prefix-f")} {blockMeta.EntityName}");
            _metaData.SetEntityName(uid, $"{Loc.GetString("gavel-broken-entityname-prefix-m")} {hammerMeta.EntityName}");

            RemComp<GavelBlockComponent>(args.Target.Value);
            RemComp<GavelHammerComponent>(uid);
        }
    }
}
