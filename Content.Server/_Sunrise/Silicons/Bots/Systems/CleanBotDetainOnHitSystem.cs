using Content.Server._Sunrise.Silicons.Bots.Components;
using Content.Shared._Sunrise.Silicons.Bots;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Silicons.Bots.Systems;

public sealed class CleanBotDetainOnHitSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CleanBotDetainOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<CleanBotDetainOnHitComponent, CleanBotDetainDoAfterEvent>(OnDetainDoAfter);
        SubscribeLocalEvent<CleanBotDetainOnHitComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
    }

    private void OnMeleeHit(Entity<CleanBotDetainOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.Enabled || ent.Comp.IsDetaining || !args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!CanDetain(target))
                continue;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Duration, new CleanBotDetainDoAfterEvent(), ent, target)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
                NeedHand = false,
                DistanceThreshold = 1f
            };

            if (!_doAfter.TryStartDoAfter(doAfterArgs))
                continue;

            ent.Comp.IsDetaining = true;
            ent.Comp.DetainEndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Duration);
            _actionBlocker.UpdateCanMove(ent.Owner);
            break;
        }
    }

    private void OnDetainDoAfter(Entity<CleanBotDetainOnHitComponent> ent, ref CleanBotDetainDoAfterEvent args)
    {
        ClearDetaining(ent);

        if (args.Cancelled || args.Handled || !ent.Comp.Enabled || args.Args.Target is not { } target || !CanDetain(target))
            return;

        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(target))
            return;

        args.Handled = true;

        var handcuffs = Spawn(ent.Comp.HandcuffPrototype, Transform(ent.Owner).Coordinates);

        if (!_cuffable.TryCuffingNow(ent.Owner, target, handcuffs))
            QueueDel(handcuffs);
    }

    private bool CanDetain(EntityUid target)
    {
        return TryComp<CuffableComponent>(target, out var cuffable)
               && cuffable.Container.Count == 0;
    }

    private void OnUpdateCanMove(Entity<CleanBotDetainOnHitComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!ent.Comp.IsDetaining)
            return;

        if (_timing.CurTime >= ent.Comp.DetainEndTime)
        {
            ClearDetaining(ent);
            return;
        }

        args.Cancel();
    }

    private void ClearDetaining(Entity<CleanBotDetainOnHitComponent> ent)
    {
        if (!ent.Comp.IsDetaining)
            return;

        ent.Comp.IsDetaining = false;
        ent.Comp.DetainEndTime = TimeSpan.Zero;
        _actionBlocker.UpdateCanMove(ent.Owner);
    }
}
