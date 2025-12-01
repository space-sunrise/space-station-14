using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Popups;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
        _alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnClickedAlert(Entity<TrackerComponent> ent, ref TrackerClickedAlertEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var currentComponent = GetCurrentComponent(ent.Comp);
        if (string.IsNullOrEmpty(currentComponent))
        {
            ent.Comp.Target = null;
            UpdateDirection(ent);
            Dirty(ent);
            return;
        }

        var targets = FindEntitiesWithComponent(currentComponent, ent.Owner);
        if (targets.Count == 0)
        {
            ent.Comp.Target = null;
            UpdateDirection(ent);
            Dirty(ent);

            _popup.PopupPredictedCursor(Loc.GetString("tracker-no-targets"), ent.Owner, PopupType.Small);
            return;
        }

        var currentIndex = ent.Comp.Target != null ? targets.IndexOf(ent.Comp.Target.Value) : -1;
        var nextIndex = (currentIndex + 1) % targets.Count;
        ent.Comp.Target = targets[nextIndex];

        var targetName = MetaData(ent.Comp.Target.Value).EntityName;

        UpdateDirection(ent, _transform.GetMapCoordinates(ent.Comp.Target.Value));
        Dirty(ent);

        _popup.PopupPredictedCursor(Loc.GetString("tracker-target-changed", ("target", targetName)), ent.Owner, PopupType.Small);
    }

    private void OnAltClickedAlert(Entity<TrackerComponent> ent, ref TrackerAltClickedAlertEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.TrackedComponents.Count <= 1)
        {
            _popup.PopupPredictedCursor(Loc.GetString("tracker-only-one-component"), ent.Owner, PopupType.Small);
            return;
        }

        var oldComponent = GetCurrentComponent(ent.Comp);

        ent.Comp.CurrentComponentIndex = (ent.Comp.CurrentComponentIndex + 1) % ent.Comp.TrackedComponents.Count;
        ent.Comp.Target = null;

        var newComponent = GetCurrentComponent(ent.Comp);

        if (oldComponent != newComponent)
            _popup.PopupPredictedCursor(Loc.GetString("tracker-component-changed", ("component", newComponent)), ent.Owner, PopupType.Small);

        UpdateDirection(ent);
        Dirty(ent);
    }

    private string GetCurrentComponent(TrackerComponent component)
    {
        if (component.TrackedComponents.Count == 0)
            return string.Empty;

        return component.TrackedComponents.ElementAt(component.CurrentComponentIndex);
    }

    private List<EntityUid> FindEntitiesWithComponent(string componentName, EntityUid exclude)
    {
        var targets = new List<EntityUid>();
        var componentType = _componentFactory.GetRegistration(componentName).Type;

        var query = EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == exclude)
                continue;

            if (EntityManager.HasComponent(uid, componentType) && !targets.Contains(uid))
                targets.Add(uid);
        }

        return targets;
    }

    private void UpdateDirection(Entity<TrackerComponent> ent, MapCoordinates? coordinates = null)
    {
        _alerts.ClearAlertCategory(ent.Owner, TrackerCategory);
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
            var currentComponent = GetCurrentComponent(tracker);

            if (tracker.Target != null)
            {
                var componentType = _componentFactory.GetRegistration(currentComponent).Type;
                if (!EntityManager.HasComponent(tracker.Target.Value, componentType))
                {
                    tracker.Target = null;
                    continue;
                }

                UpdateDirection((uid, tracker), _transform.GetMapCoordinates(tracker.Target.Value));
                continue;
            }

            FindNewTarget((uid, tracker), currentComponent);
            UpdateDirection((uid, tracker));
        }
    }

    private void FindNewTarget(Entity<TrackerComponent> ent, string componentName)
    {
        if (string.IsNullOrEmpty(componentName))
        {
            ent.Comp.Target = null;
            return;
        }

        var componentType = _componentFactory.GetRegistration(componentName).Type;
        var shortestDistance = float.MaxValue;
        EntityUid? closestTarget = null;

        var query = EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == ent.Owner || !EntityManager.HasComponent(uid, componentType))
                continue;

            var distance = (_transform.GetWorldPosition(ent.Owner) -
                            _transform.GetWorldPosition(uid)).LengthSquared();

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestTarget = uid;
            }
        }

        ent.Comp.Target = closestTarget;
    }
}
