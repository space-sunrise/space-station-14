using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.Player;
using Robust.Client.Input;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SiliconRestingVisualizerSystem _visualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<SiliconStandingTransitionComponent, ComponentStartup>(OnTransitionState);
        SubscribeLocalEvent<SiliconStandingTransitionComponent, AfterAutoHandleStateEvent>(OnTransitionStateHandled);
        SubscribeLocalEvent<SiliconStandingTransitionComponent, ComponentShutdown>(OnTransitionShutdown);
        SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnPhysicsBeforeSolve, before: [typeof(Content.Shared.Movement.Systems.SharedMoverController)]);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding,
                new PointerInputCmdHandler((session, coords, uid) =>
                {
                    var ent = _player.LocalEntity;

                    if (ent != null && HasComp<BorgChassisComponent>(ent.Value))
                    {
                        SendToggleEvent();
                        return true;
                    }

                    return false;
                }))
            .Register<SiliconStandingSystem>();
    }

    /// <summary>
    /// Sends a toggle request to the server if the player controls a borg.
    /// </summary>
    private void SendToggleEvent()
    {
        var uid = _player.LocalEntity;

        if (uid == null)
            return;

        if (!HasComp<BorgChassisComponent>(uid.Value))
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        StartPredictedTransition(uid.Value);
        RaiseNetworkEvent(new ToggleStandingEvent());
    }

    public bool GetEffectiveResting(EntityUid uid)
    {
        if (TryComp<SiliconStandingTransitionComponent>(uid, out var transition) && transition.Completed)
            return transition.TargetResting;

        return HasComp<SiliconRestingComponent>(uid);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SiliconStandingSystem>();
    }

    private void OnCanMove(Entity<BorgChassisComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (GetEffectiveResting(ent.Owner))
            args.Cancel();
    }

    private void OnTransitionState(Entity<SiliconStandingTransitionComponent> ent, ref ComponentStartup args)
    {
        RefreshPredictedState(ent.Owner);
    }

    private void OnTransitionStateHandled(Entity<SiliconStandingTransitionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshPredictedState(ent.Owner);
    }

    private void OnTransitionShutdown(Entity<SiliconStandingTransitionComponent> ent, ref ComponentShutdown args)
    {
        if (!_timing.ApplyingState)
            RefreshPredictedState(ent.Owner);
    }

    private void StartPredictedTransition(EntityUid uid)
    {
        if (!TryComp<SiliconStandingComponent>(uid, out var standing))
            return;

        if (TryComp<SiliconStandingTransitionComponent>(uid, out _))
            return;

        var resting = HasComp<SiliconRestingComponent>(uid);
        var targetResting = !resting;
        var delay = TimeSpan.FromSeconds(targetResting ? standing.LieDownDelay : standing.StandUpDelay);
        var transition = EnsureComp<SiliconStandingTransitionComponent>(uid);
        transition.TargetResting = targetResting;
        transition.EndTime = _timing.CurTime + delay;
        transition.Completed = false;
        Dirty(uid, transition);
        RefreshPredictedState(uid);
    }

    private void OnPhysicsBeforeSolve(ref PhysicsUpdateBeforeSolveEvent args)
    {
        if (_player.LocalEntity is not { Valid: true } uid)
            return;

        if (!TryComp<SiliconStandingTransitionComponent>(uid, out var transition))
            return;

        if (transition.Completed || _timing.CurTime < transition.EndTime)
            return;

        transition.Completed = true;
        Dirty(uid, transition);
        RefreshPredictedState(uid);
    }

    private void RefreshPredictedState(EntityUid uid)
    {
        _actionBlocker.UpdateCanMove(uid);
        _visualizer.Refresh(uid);
    }
}
