using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.DoAfter;
using Content.Shared.ActionBlocker;
using Content.Server.DoAfter;
using Robust.Server.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.SiliconStanding;

/// <summary>
/// Handles borg resting/standing state transitions using DoAfter.
/// Manages movement blocking and appearance updates.
/// </summary>
public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ToggleStandingEvent>(OnToggle);
        SubscribeLocalEvent<BorgChassisComponent, SiliconRestingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SiliconRestingComponent, UpdateCanMoveEvent>(OnCanMove);
    }

    /// <summary>
    /// Handles client request to toggle borg resting state.
    /// Starts a DoAfter action with delay depending on current state.
    /// </summary>
    private void OnToggle(ToggleStandingEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        TryToggleResting(uid);
    }

    public bool IsResting(EntityUid uid)
    {
        return HasComp<SiliconRestingComponent>(uid);
    }

    public void SetResting(EntityUid uid, bool resting)
    {
        if (resting)
            EnsureComp<SiliconRestingComponent>(uid);
        else
            RemComp<SiliconRestingComponent>(uid);

        _actionBlocker.UpdateCanMove(uid);
    }

    public bool TryToggleResting(EntityUid uid)
    {
        if (!TryComp<SiliconStandingComponent>(uid, out var standing))
            return false;

        if (!HasComp<BorgChassisComponent>(uid))
            return false;

        var resting = !IsResting(uid);
        var delay = resting ? standing.LieDownDelay : standing.StandUpDelay;
        SetTransition(uid, resting, TimeSpan.FromSeconds(delay));

        var doAfter = new DoAfterArgs(
            EntityManager,
            uid,
            delay,
            new SiliconRestingDoAfterEvent(),
            uid,
            uid
        )
        {
            BreakOnMove = true,
            Broadcast = true,
            BlockDuplicate = true
        };

        var started = _doAfter.TryStartDoAfter(doAfter);

        if (!started)
            RemComp<SiliconStandingTransitionComponent>(uid);

        return started;
    }

    private void OnCanMove(Entity<SiliconRestingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnDoAfter(Entity<BorgChassisComponent> ent, ref SiliconRestingDoAfterEvent ev)
    {
        RemComp<SiliconStandingTransitionComponent>(ent);

        if (ev.Cancelled)
            return;

        SetResting(ent, !IsResting(ent));

        if (TryComp<FootstepModifierComponent>(ent, out var footsteps))
            _audio.PlayPvs(footsteps.FootstepSoundCollection, ent);
    }

    private void SetTransition(EntityUid uid, bool targetResting, TimeSpan duration)
    {
        var transition = EnsureComp<SiliconStandingTransitionComponent>(uid);
        transition.TargetResting = targetResting;
        transition.EndTime = _timing.CurTime + duration;
        Dirty(uid, transition);
    }
}
