using Content.Shared._Sunrise.Antags.Vampires.Events;
using System.Numerics;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;
using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Antags.Vampires.Systems;

/// <summary>
/// Client-side system for smooth vampire beam visualizations.
/// </summary>
public sealed class VampireBeamSystem : EntitySystem
{
    private enum BeamKind
    {
        Drain,
        BloodBond,
    }

    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<VampireBeamVisualComponent> _beamVisualQuery;

    private readonly Dictionary<(BeamKind Kind, EntityUid Source, EntityUid Target), EntityUid> _activeBeamVisuals = [];
    private readonly List<(BeamKind Kind, EntityUid Source, EntityUid Target)> _toRemove = [];

    public override void Initialize()
    {
        base.Initialize();
        _beamVisualQuery = GetEntityQuery<VampireBeamVisualComponent>();
        SubscribeNetworkEvent<VampireDrainBeamEvent>(OnDrainBeamEvent);
        SubscribeNetworkEvent<VampireBloodBondBeamEvent>(OnBloodBondBeamEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateActiveBeamVisuals();
        UpdatePredictedBloodBondBeams();
    }

    public override void Shutdown()
    {
        foreach (var beamEntity in _activeBeamVisuals.Values)
        {
            if (Exists(beamEntity))
                QueueDel(beamEntity);
        }

        _activeBeamVisuals.Clear();
        base.Shutdown();
    }

    private void OnDrainBeamEvent(VampireDrainBeamEvent ev)
        => HandleBeamEvent(ev.Source, ev.Target, ev.Create, BeamKind.Drain, ev.VisualPrototype, replaceExisting: true);

    private void OnBloodBondBeamEvent(VampireBloodBondBeamEvent ev)
        => HandleBeamEvent(ev.Source, ev.Target, ev.Create, BeamKind.BloodBond, ev.VisualPrototype, replaceExisting: false);

    private void HandleBeamEvent(
        NetEntity sourceNet,
        NetEntity targetNet,
        bool create,
        BeamKind kind,
        string prototype,
        bool replaceExisting)
    {
        var source = GetEntity(sourceNet);
        var target = GetEntity(targetNet);

        if (!Exists(source) || !Exists(target))
            return;

        if (create)
        {
            CreateBeamVisual(kind, source, target, prototype, replaceExisting);
            return;
        }

        RemoveBeamVisual(kind, source, target);
    }

    private void UpdateActiveBeamVisuals()
    {
        _toRemove.Clear();

        foreach (var ((kind, source, target), beamEntity) in _activeBeamVisuals)
        {
            if (!Exists(source) || !Exists(target) || !Exists(beamEntity))
            {
                _toRemove.Add((kind, source, target));

                if (Exists(beamEntity))
                    QueueDel(beamEntity);

                continue;
            }

            UpdateBeamVisual(beamEntity, source, target);
        }

        foreach (var key in _toRemove)
        {
            _activeBeamVisuals.Remove(key);
        }
    }

    private void UpdatePredictedBloodBondBeams()
    {
        var dantalions = EntityQueryEnumerator<DantalionComponent>();
        while (dantalions.MoveNext(out var source, out var dantalion))
        {
            if (!dantalion.BloodBondActive)
            {
                RemoveSourceBeams(BeamKind.BloodBond, source);
                continue;
            }

            var linkedThralls = dantalion.BloodBondLinkedThralls;
            RemoveUnlinkedSourceBeams(BeamKind.BloodBond, source, linkedThralls);

            foreach (var target in linkedThralls)
            {
                if (!Exists(target))
                    continue;

                CreateBeamVisual(BeamKind.BloodBond, source, target, dantalion.BloodBondBeamPrototype, replaceExisting: false);
            }
        }
    }

    private void CreateBeamVisual(
        BeamKind kind,
        EntityUid source,
        EntityUid target,
        string visualPrototype,
        bool replaceExisting)
    {
        var key = (kind, source, target);

        if (_activeBeamVisuals.TryGetValue(key, out var existingBeam))
        {
            if (Exists(existingBeam) && !replaceExisting)
                return;

            if (Exists(existingBeam))
                QueueDel(existingBeam);
        }

        var beam = Spawn(visualPrototype, Transform(source).Coordinates);
        _activeBeamVisuals[key] = beam;

        UpdateBeamVisual(beam, source, target);
    }

    private void RemoveBeamVisual(BeamKind kind, EntityUid source, EntityUid target)
    {
        var key = (kind, source, target);
        if (!_activeBeamVisuals.Remove(key, out var beamEntity))
            return;

        if (Exists(beamEntity))
            QueueDel(beamEntity);
    }

    private void RemoveSourceBeams(BeamKind kind, EntityUid source)
    {
        _toRemove.Clear();
        foreach (var ((beamKind, beamSource, target), beamEntity) in _activeBeamVisuals)
        {
            if (beamKind != kind || beamSource != source)
                continue;

            if (Exists(beamEntity))
                QueueDel(beamEntity);

            _toRemove.Add((beamKind, beamSource, target));
        }

        foreach (var key in _toRemove)
        {
            _activeBeamVisuals.Remove(key);
        }
    }

    private void RemoveUnlinkedSourceBeams(BeamKind kind, EntityUid source, List<EntityUid> linkedThralls)
    {
        _toRemove.Clear();
        foreach (var ((beamKind, beamSource, target), beamEntity) in _activeBeamVisuals)
        {
            if (beamKind != kind || beamSource != source || linkedThralls.Contains(target))
                continue;

            if (Exists(beamEntity))
                QueueDel(beamEntity);

            _toRemove.Add((beamKind, beamSource, target));
        }

        foreach (var key in _toRemove)
        {
            _activeBeamVisuals.Remove(key);
        }
    }

    private void UpdateBeamVisual(EntityUid beam, EntityUid source, EntityUid target)
    {
        if (!TryComp<SpriteComponent>(beam, out var sprite)
            || !_beamVisualQuery.TryComp(beam, out var beamVisual))
        {
            return;
        }

        var sourcePos = _transform.GetWorldPosition(source);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = targetPos - sourcePos;
        var distance = direction.Length();

        if (distance < beamVisual.MinDistance)
            return;

        var worldAngle = direction.ToWorldAngle() + beamVisual.AngleOffset;

        var midpoint = sourcePos + (direction * 0.5f);
        _transform.SetWorldPosition(beam, midpoint);

        _transform.SetWorldRotation(beam, worldAngle);
        _sprite.SetRotation((beam, sprite), Angle.Zero);

        var length = MathF.Max(beamVisual.MinLength, distance);
        var scale = beamVisual.SpriteIsVertical
            ? new Vector2(beamVisual.Thickness, length)
            : new Vector2(length, beamVisual.Thickness);
        _sprite.SetScale((beam, sprite), scale);
        _sprite.SetOffset((beam, sprite), Vector2.Zero);
    }
}
