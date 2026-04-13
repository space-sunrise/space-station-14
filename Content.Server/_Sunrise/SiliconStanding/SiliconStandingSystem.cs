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
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestingShutdown);
    }

    /// <summary>
    /// Handles client request to toggle borg resting state.
    /// Starts a DoAfter action with delay depending on current state.
    /// </summary>
    private void OnToggle(ToggleStandingEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        if (!HasComp<BorgChassisComponent>(uid))
            return;

        var delay = HasComp<SiliconRestingComponent>(uid) ? 0.5f : 1.0f;

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

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void SetResting(EntityUid uid, bool resting)
    {
        if (resting)
            EnsureComp<SiliconRestingComponent>(uid);
        else
            RemComp<SiliconRestingComponent>(uid);
    }

    private void Toggle(EntityUid uid)
    {
        SetResting(uid, !HasComp<SiliconRestingComponent>(uid));
    }

    private void OnCanMove(Entity<SiliconRestingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnRestingStartup(Entity<SiliconRestingComponent> ent, ref ComponentStartup args)
    {
        var appearance = EnsureComp<AppearanceComponent>(ent);
        _appearance.SetData(ent.Owner, SiliconStandingVisuals.Resting, true, appearance);
        _actionBlocker.UpdateCanMove(ent);
    }

    private void OnRestingShutdown(Entity<SiliconRestingComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.RemoveData(ent.Owner, SiliconStandingVisuals.Resting, appearance);

        _actionBlocker.UpdateCanMove(ent);
    }

    private void OnDoAfter(Entity<BorgChassisComponent> ent, ref SiliconRestingDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        Toggle(ent);

        if (TryComp<FootstepModifierComponent>(ent, out var footsteps))
            _audio.PlayPvs(footsteps.FootstepSoundCollection, ent);
    }
}
