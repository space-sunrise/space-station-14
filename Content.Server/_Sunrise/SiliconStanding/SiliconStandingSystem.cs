using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.DoAfter;
using Content.Shared.ActionBlocker;
using Content.Server.DoAfter;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server._Sunrise.SiliconStanding;

/// <summary>
/// Handles borg resting/standing state transitions using DoAfter.
/// Manages movement blocking and appearance updates.
/// </summary>
public sealed class SiliconStandingSystem : EntitySystem
{
    private const float LieDownDelay = 1.0f;
    private const float StandUpDelay = 0.5f;

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ToggleStandingEvent>(OnToggle);
        SubscribeLocalEvent<BorgChassisComponent, SiliconRestingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SiliconRestingComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentRemove>(OnRestingRemove);
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
        if (!HasComp<BorgChassisComponent>(uid))
            return false;

        var resting = !IsResting(uid);
        var attempt = new SiliconRestToggleAttemptEvent(resting);
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        var delayEv = new GetSiliconRestDelayEvent(resting, resting ? LieDownDelay : StandUpDelay);
        RaiseLocalEvent(uid, ref delayEv);

        var doAfter = new DoAfterArgs(
            EntityManager,
            uid,
            delayEv.Delay,
            new SiliconRestingDoAfterEvent(),
            uid,
            uid
        )
        {
            BreakOnMove = true,
            Broadcast = true,
            BlockDuplicate = true
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnCanMove(Entity<SiliconRestingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnRestingStartup(Entity<SiliconRestingComponent> ent, ref ComponentStartup args)
    {
        var appearance = EnsureComp<AppearanceComponent>(ent);
        _appearance.SetData(ent.Owner, SiliconStandingVisuals.Resting, true, appearance);
    }

    private void OnRestingRemove(Entity<SiliconRestingComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.RemoveData(ent.Owner, SiliconStandingVisuals.Resting, appearance);
    }

    private void OnDoAfter(Entity<BorgChassisComponent> ent, ref SiliconRestingDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        SetResting(ent, !IsResting(ent));

        if (TryComp<FootstepModifierComponent>(ent, out var footsteps))
            _audio.PlayPvs(footsteps.FootstepSoundCollection, ent);
    }
}
