using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.DoAfter;
using Content.Shared.ActionBlocker;
using Content.Server.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;

namespace Content.Server._Sunrise.SiliconStanding;

/// <summary>
/// Handles borg resting/standing state transitions using DoAfter.
/// Manages movement blocking, appearance updates, and input handling.
/// </summary>
public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    private const string ToggleSound = "/Audio/Effects/Footsteps/borgwalk1.ogg";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ToggleStandingEvent>(OnToggle);
        SubscribeLocalEvent<SiliconRestingDoAfterComponent, SiliconRestingDoAfterEvent>(OnDoAfter);
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

        if (!HasComp<BorgChassisComponent>(uid))
            return;

        if (HasComp<SiliconRestingDoAfterComponent>(uid))
            return;
            
        var isResting  = HasComp<SiliconRestingComponent>(uid);
        var delay = isResting  ? 0.5f : 1.0f;

        EnsureComp<SiliconRestingDoAfterComponent>(uid);

        var doAfter = new DoAfterArgs(
            EntityManager,
            uid,
            delay,
            new SiliconRestingDoAfterEvent(),
            uid,
            null
        )
        {
            BreakOnMove = true,
            Broadcast = true,
            BlockDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            RemComp<SiliconRestingDoAfterComponent>(uid);
        }
    }

    /// <summary>
    /// Applies resting or standing state to the borg.
    /// Updates appearance and recalculates movement permissions.
    /// </summary>
    private void SetResting(EntityUid uid, bool resting)
    {
        if (resting)
        {   
            EnsureComp<SiliconRestingComponent>(uid);
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, true);
        }
        else
        {
            RemComp<SiliconRestingComponent>(uid);
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, false);
        }

        _actionBlocker.UpdateCanMove(uid);
    }

    /// <summary>
    /// Toggles borg between resting and standing states.
    /// </summary>
    private void Toggle(EntityUid uid)
    {
        var resting = !HasComp<SiliconRestingComponent>(uid);
        SetResting(uid, resting);
    }

    private void OnCanMove(EntityUid uid, SiliconRestingComponent component, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Called when DoAfter completes.
    /// Applies the new state and plays the toggle sound.
    /// </summary>
    private void OnDoAfter(EntityUid uid, SiliconRestingDoAfterComponent comp, SiliconRestingDoAfterEvent ev)
    {
        RemComp<SiliconRestingDoAfterComponent>(uid);

        if (ev.Cancelled)
            return;

        Toggle(uid);

        _audio.PlayPvs(ToggleSound, uid);
    }
}
