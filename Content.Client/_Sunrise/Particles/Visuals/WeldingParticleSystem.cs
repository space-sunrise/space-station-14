using Content.Shared._Sunrise.Particles;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Directs welding sparks from the contact point toward the welder and adds a compact completion flash.
/// </summary>
public sealed class WeldingParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly ParticleVisualAnchorSystem _anchors = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<ParticleOrchestraPrototype> ContinuousOrchestra = "WeldingContinuous";
    private static readonly ProtoId<ParticleOrchestraPrototype> CompletionOrchestra = "WeldingCompletion";
    private static readonly ProtoId<ToolQualityPrototype> WeldingQuality = "Welding";
    private static readonly TimeSpan CompletionDeduplicationTime = TimeSpan.FromSeconds(2);

    private readonly Dictionary<DoAfterId, WeldingState> _activeWelds = new();
    private readonly Dictionary<DoAfterId, TimeSpan> _recentCompletions = new();
    private readonly HashSet<DoAfterId> _seenDoAfters = new();
    private readonly List<DoAfterId> _staleDoAfters = new();
    private readonly List<DoAfterId> _staleCompletions = new();

    private EntityQuery<WelderComponent> _welderQuery;

    public override void Initialize()
    {
        base.Initialize();

        _welderQuery = GetEntityQuery<WelderComponent>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var state in _activeWelds.Values)
        {
            _orchestra.Stop(state.Orchestra);
        }

        _activeWelds.Clear();
        _recentCompletions.Clear();
        _seenDoAfters.Clear();
        _staleDoAfters.Clear();
        _staleCompletions.Clear();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var curTime = _timing.CurTime;
        _seenDoAfters.Clear();

        var query = EntityQueryEnumerator<ActiveDoAfterComponent, DoAfterComponent>();
        while (query.MoveNext(out _, out _, out var doAfterComponent))
        {
            foreach (var doAfter in doAfterComponent.DoAfters.Values)
            {
                var id = doAfter.Id;
                if (doAfter.Cancelled)
                    continue;

                if (doAfter.Completed)
                {
                    if (_activeWelds.TryGetValue(id, out var completed) &&
                        !_recentCompletions.ContainsKey(id))
                    {
                        SpawnWeldingEffects(completed.Target, completed.User);
                        _recentCompletions[id] = curTime + CompletionDeduplicationTime;
                    }

                    continue;
                }

                if (doAfter.Args.Used is not { } used ||
                    !_welderQuery.TryComp(used, out var welder) ||
                    !welder.Enabled)
                {
                    continue;
                }

                if (!_tool.HasQuality(used, WeldingQuality))
                    continue;

                if (doAfter.Args.Target is not { } target || TerminatingOrDeleted(target))
                    continue;

                var user = doAfter.Args.User;
                if (TerminatingOrDeleted(user))
                    continue;

                var targetCoordinates = _transform.GetMapCoordinates(target);
                var userCoordinates = _transform.GetMapCoordinates(user);
                if (targetCoordinates.MapId != userCoordinates.MapId)
                    continue;

                var welderPoint = userCoordinates.Position + _anchors.GetOffset(user, ParticleVisualAnchor.Hands);
                var contactOffset = _anchors.GetVisualEdgeOffset(target, welderPoint);
                _seenDoAfters.Add(id);

                if (_activeWelds.TryGetValue(id, out var active))
                {
                    _orchestra.UpdateSpawnOffset(active.Orchestra, contactOffset);
                    _orchestra.UpdateTargetPosition(active.Orchestra, welderPoint);
                    continue;
                }

                var orchestra = _orchestra.StartAt(
                    ContinuousOrchestra,
                    targetCoordinates,
                    target,
                    user,
                    spawnOffset: contactOffset);
                if (orchestra == null)
                    continue;

                _orchestra.UpdateTargetPosition(orchestra, welderPoint);
                _activeWelds[id] = new WeldingState(orchestra, target, user);
            }
        }

        RemoveStoppedEmitters();
        RemoveExpiredCompletions(curTime);
    }

    private void SpawnWeldingEffects(EntityUid target, EntityUid user)
    {
        if (TerminatingOrDeleted(target) || TerminatingOrDeleted(user))
            return;

        var targetCoordinates = _transform.GetMapCoordinates(target);
        var userCoordinates = _transform.GetMapCoordinates(user);
        if (targetCoordinates.MapId != userCoordinates.MapId)
            return;

        var welderPoint = userCoordinates.Position + _anchors.GetOffset(user, ParticleVisualAnchor.Hands);
        var contactOffset = _anchors.GetVisualEdgeOffset(target, welderPoint);
        var contactPoint = targetCoordinates.Position + contactOffset;
        _orchestra.SpawnAt(
            CompletionOrchestra,
            new MapCoordinates(contactPoint, targetCoordinates.MapId),
            user,
            target,
            welderPoint - contactPoint);
    }

    private void RemoveStoppedEmitters()
    {
        _staleDoAfters.Clear();

        foreach (var id in _activeWelds.Keys)
        {
            if (!_seenDoAfters.Contains(id))
                _staleDoAfters.Add(id);
        }

        foreach (var id in _staleDoAfters)
        {
            _orchestra.Stop(_activeWelds[id].Orchestra);
            _activeWelds.Remove(id);
        }
    }

    private void RemoveExpiredCompletions(TimeSpan curTime)
    {
        _staleCompletions.Clear();

        foreach (var (id, expiry) in _recentCompletions)
        {
            if (expiry <= curTime)
                _staleCompletions.Add(id);
        }

        foreach (var id in _staleCompletions)
        {
            _recentCompletions.Remove(id);
        }
    }

    private readonly record struct WeldingState(
        ActiveParticleOrchestra Orchestra,
        EntityUid Target,
        EntityUid User);
}
