using System.Diagnostics.CodeAnalysis;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;

namespace Content.Shared._Sunrise.Movement.Standing.Systems;

public abstract partial class SharedSunriseStandingStateSystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;

    private EntityQuery<PullableComponent> _pullableQuery;

    private void InitializePronePulling()
    {
        _pullableQuery = GetEntityQuery<PullableComponent>();

        SubscribeLocalEvent<ActivePullerComponent, DownedEvent>(OnPullerDowned);
        SubscribeLocalEvent<StandingStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
    }

    private void OnPullerDowned(Entity<ActivePullerComponent> ent, ref DownedEvent args)
    {
        TryStopPulling(ent);
    }

    private void OnStartPullAttempt(Entity<StandingStateComponent> ent, ref StartPullAttemptEvent args)
    {
        if (args.Cancelled || CanStartPull(ent))
            return;

        args.Cancel();
    }

    public bool TryStopPulling(EntityUid ent)
    {
        if (!ShouldStopPulling(ent, out var pulledUid, out var pullable))
            return false;

        return DoStopPulling(ent, pulledUid, pullable);
    }

    public bool CanStartPull(Entity<StandingStateComponent> ent)
    {
        return ent.Comp.Standing;
    }

    private bool ShouldStopPulling(
        Entity<StandingStateComponent?> ent,
        out EntityUid pulledUid,
        [NotNullWhen(true)] out PullableComponent? pullable)
    {
        pulledUid = default;
        pullable = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Standing)
            return false;

        var pulling = _pulling.GetPulling(ent.Owner);
        if (pulling == null || !_pullableQuery.TryComp(pulling.Value, out pullable))
            return false;

        pulledUid = pulling.Value;
        return true;
    }

    private bool DoStopPulling(EntityUid pullerUid, EntityUid pulledUid, PullableComponent pullable)
    {
        return _pulling.TryStopPull(pulledUid, pullable, pullerUid);
    }
}
