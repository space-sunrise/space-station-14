using Content.Shared.Alert;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Tracker;

public sealed class TrackerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DirectionTrackerSystem _directionTracker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string TrackerCategory = "Tracker";

    public override void Initialize()
    {
        SubscribeLocalEvent<TrackerComponent, ComponentRemove>(OnRemove);
        base.Initialize();
    }

    private void OnRemove(Entity<TrackerComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void UpdateDirection(Entity<TrackerComponent> ent, MapCoordinates? coordinates = null)
    {
        _alerts.ClearAlertCategory(ent, TrackerCategory);
        var severity = DirectionTrackerSystem.CenterSeverity;

        if (coordinates != null)
            severity = _directionTracker.GetAlertSeverity(ent.Owner, coordinates.Value);

        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert, severity);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<TrackerComponent>();

        while (query.MoveNext(out var uid, out var tracker))
        {
            if (time < tracker.UpdateAt)
                continue;

            tracker.UpdateAt = time + tracker.UpdateEvery;

            if (tracker.Target != null)
            {
                if (!HasAnyTrackedComponent(tracker.Target.Value, tracker.TrackedComponents))
                {
                    tracker.Target = null;
                    continue;
                }

                UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value));
                continue;
            }

            FindNewTarget((uid, tracker));
            UpdateDirection((uid, tracker));
        }
    }

    private bool HasAnyTrackedComponent(EntityUid entity, HashSet<string> trackedComponents)
    {
        foreach (var componentName in trackedComponents)
        {
            var componentType = _componentFactory.GetRegistration(componentName).Type;
            if (EntityManager.HasComponent(entity, componentType))
                return true;
        }
        return false;
    }

    private void FindNewTarget(Entity<TrackerComponent> ent)
    {
        var trackableQuery = EntityQueryEnumerator<MetaDataComponent>();
        var shortestDistance = float.MaxValue;
        EntityUid? closestTarget = null;

        while (trackableQuery.MoveNext(out var trackableUid, out _))
        {
            if (trackableUid == ent.Owner || !HasAnyTrackedComponent(trackableUid, ent.Comp.TrackedComponents))
                continue;

            var distance = (_transform.GetWorldPosition(ent.Owner) -
                           _transform.GetWorldPosition(trackableUid)).LengthSquared();

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestTarget = trackableUid;
            }
        }

        ent.Comp.Target = closestTarget;
    }
}
