using System.Linq;
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
        SubscribeLocalEvent<TrackerComponent, TrackerClickedAlertEvent>(OnClickedAlert);
        SubscribeLocalEvent<TrackerComponent, TrackerAltClickedAlertEvent>(OnAltClickedAlert);
        base.Initialize();
    }

    private void OnRemove(Entity<TrackerComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnClickedAlert(Entity<TrackerComponent> ent, ref TrackerClickedAlertEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var trackedComponents = GetCurrentTrackedComponents(ent.Comp);

        var targets = FindAllTrackedEntities(trackedComponents, ent.Owner);
        if (targets.Count == 0)
        {
            ent.Comp.Target = null;
            UpdateDirection(ent);
            Dirty(ent);
            return;
        }

        var currentIndex = ent.Comp.Target != null ? targets.IndexOf(ent.Comp.Target.Value) : -1;
        var nextIndex = (currentIndex + 1) % targets.Count;
        ent.Comp.Target = targets[nextIndex];

        UpdateDirection(ent, _transform.GetMapCoordinates(ent.Comp.Target.Value));
        Dirty(ent);
    }

    private void OnAltClickedAlert(Entity<TrackerComponent> ent, ref TrackerAltClickedAlertEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.TrackingModes.Count == 0)
            return;

        var modes = ent.Comp.TrackingModes.Keys.ToList();
        var currentIndex = modes.IndexOf(ent.Comp.CurrentMode);
        var nextIndex = (currentIndex + 1) % modes.Count;

        ent.Comp.CurrentMode = modes[nextIndex];
        ent.Comp.Target = null;

        UpdateDirection(ent);
        Dirty(ent);
    }

    private HashSet<string> GetCurrentTrackedComponents(TrackerComponent component)
    {
        return component.TrackingModes.TryGetValue(component.CurrentMode, out var trackedComponents)
            ? trackedComponents
            : component.TrackedComponents;
    }

    private List<EntityUid> FindAllTrackedEntities(HashSet<string> trackedComponents, EntityUid exclude)
    {
        var targets = new List<EntityUid>();
        var allEntities = EntityQuery<MetaDataComponent>().Select(e => e.Owner).ToList();

        foreach (var entity in allEntities)
        {
            if (entity == exclude)
                continue;

            if (HasAnyTrackedComponent(entity, trackedComponents) && !targets.Contains(entity))
            {
                targets.Add(entity);
            }
        }
        return targets;
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
            var trackedComponents = GetCurrentTrackedComponents(tracker);

            if (tracker.Target != null)
            {
                if (!HasAnyTrackedComponent(tracker.Target.Value, trackedComponents))
                {
                    tracker.Target = null;
                    continue;
                }

                UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value));
                continue;
            }

            FindNewTarget((uid, tracker), trackedComponents);
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

    private void FindNewTarget(Entity<TrackerComponent> ent, HashSet<string> trackedComponents)
    {
        var trackableQuery = EntityQueryEnumerator<MetaDataComponent>();
        var shortestDistance = float.MaxValue;
        EntityUid? closestTarget = null;

        while (trackableQuery.MoveNext(out var trackableUid, out _))
        {
            if (trackableUid == ent.Owner || !HasAnyTrackedComponent(trackableUid, trackedComponents))
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
