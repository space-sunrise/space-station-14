using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.Player;
using Robust.Client.Input;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    private const float TransitionMovementThreshold = 0.3f;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SiliconRestingVisualizerSystem _visualizer = default!;

    private readonly Dictionary<EntityUid, PredictedTransition> _predictedTransitions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestingShutdown);
        SubscribeLocalEvent<SiliconStandingTransitionComponent, ComponentStartup>(OnTransitionState);
        SubscribeLocalEvent<SiliconStandingTransitionComponent, AfterAutoHandleStateEvent>(OnTransitionStateHandled);
        SubscribeLocalEvent<SiliconStandingTransitionComponent, ComponentShutdown>(OnTransitionShutdown);

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
        if (_predictedTransitions.TryGetValue(uid, out var transition) && transition.Completed)
            return transition.TargetResting;

        return HasComp<SiliconRestingComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_predictedTransitions.Count == 0)
            return;

        var completed = new List<EntityUid>();
        var cancelled = new List<EntityUid>();

        foreach (var (uid, transition) in _predictedTransitions)
        {
            if (!Exists(uid))
            {
                cancelled.Add(uid);
                continue;
            }

            if (transition.Completed &&
                !HasComp<SiliconStandingTransitionComponent>(uid) &&
                HasComp<SiliconRestingComponent>(uid) == transition.TargetResting)
            {
                cancelled.Add(uid);
                continue;
            }

            if (!transition.Completed &&
                transition.TargetResting &&
                !_timing.ApplyingState &&
                Transform(uid).Coordinates.TryDistance(EntityManager, transition.StartCoordinates, out var distance) &&
                distance > TransitionMovementThreshold)
            {
                cancelled.Add(uid);
                continue;
            }

            if (!transition.Completed && _timing.CurTime >= transition.EndTime)
                completed.Add(uid);
        }

        foreach (var uid in cancelled)
        {
            _predictedTransitions.Remove(uid);
            RefreshPredictedState(uid);
        }

        foreach (var uid in completed)
        {
            var transition = _predictedTransitions[uid];
            transition.Completed = true;
            _predictedTransitions[uid] = transition;
            RefreshPredictedState(uid);
        }
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

    private void OnRestingStartup(Entity<SiliconRestingComponent> ent, ref ComponentStartup args)
    {
        if (_predictedTransitions.TryGetValue(ent.Owner, out var transition) &&
            transition.Completed &&
            transition.TargetResting)
            _predictedTransitions.Remove(ent.Owner);

        RefreshPredictedState(ent.Owner);
    }

    private void OnRestingShutdown(Entity<SiliconRestingComponent> ent, ref ComponentShutdown args)
    {
        if (_predictedTransitions.TryGetValue(ent.Owner, out var transition) &&
            transition.Completed &&
            !transition.TargetResting)
            _predictedTransitions.Remove(ent.Owner);

        RefreshPredictedState(ent.Owner);
    }

    private void OnTransitionState(Entity<SiliconStandingTransitionComponent> ent, ref ComponentStartup args)
    {
        ConfirmPredictedTransition(ent);
    }

    private void OnTransitionStateHandled(Entity<SiliconStandingTransitionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ConfirmPredictedTransition(ent);
    }

    private void OnTransitionShutdown(Entity<SiliconStandingTransitionComponent> ent, ref ComponentShutdown args)
    {
        if (!_predictedTransitions.TryGetValue(ent.Owner, out var predicted))
        {
            if (!_timing.ApplyingState)
                RefreshPredictedState(ent.Owner);
            return;
        }

        if (!predicted.Completed)
        {
            _predictedTransitions.Remove(ent.Owner);

            if (!_timing.ApplyingState)
                RefreshPredictedState(ent.Owner);
        }
    }

    private void StartPredictedTransition(EntityUid uid)
    {
        if (!TryComp<SiliconStandingComponent>(uid, out var standing))
            return;

        if (_predictedTransitions.ContainsKey(uid))
            return;

        var resting = HasComp<SiliconRestingComponent>(uid);
        var targetResting = !resting;
        var delay = TimeSpan.FromSeconds(targetResting ? standing.LieDownDelay : standing.StandUpDelay);

        _predictedTransitions[uid] = new PredictedTransition(
            targetResting,
            _timing.CurTime + delay,
            Transform(uid).Coordinates,
            false);
    }

    private void ConfirmPredictedTransition(Entity<SiliconStandingTransitionComponent> ent)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        if (_predictedTransitions.TryGetValue(ent.Owner, out var predicted))
        {
            _predictedTransitions[ent.Owner] = predicted;
            return;
        }

        _predictedTransitions[ent.Owner] = new PredictedTransition(
            ent.Comp.TargetResting,
            ent.Comp.EndTime,
            Transform(ent.Owner).Coordinates,
            false);
    }

    private void RefreshPredictedState(EntityUid uid)
    {
        _actionBlocker.UpdateCanMove(uid);
        _visualizer.Refresh(uid);
    }

    private record struct PredictedTransition(
        bool TargetResting,
        TimeSpan EndTime,
        EntityCoordinates StartCoordinates,
        bool Completed);
}
