using System;
using System.Numerics;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Starlight.Antags.Vampires;

/// <summary>
/// Client-side system for smooth vampire beams visualization
/// </summary>
public sealed class VampireDrainBeamSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly Angle BeamAngleOffset = Angle.FromDegrees(180); // suck em
    private const bool SpriteIsVertical = true;

    private const string DrainPrototype = "VampireDrainBeamVisual";
    private const string BloodBondPrototype = "VampireBloodBondBeamVisual";

    private readonly Dictionary<BeamKey, EntityUid> _activeBeamVisuals = new();
    private readonly List<BeamKey> _removeBuffer = new();

    private enum BeamKind
    {
        Drain,
        BloodBond
    }

    private readonly struct BeamKey(BeamKind kind, EntityUid source, EntityUid target) : IEquatable<BeamKey>
    {
        public readonly BeamKind Kind = kind;
        public readonly EntityUid Source = source;
        public readonly EntityUid Target = target;

        public bool Equals(BeamKey other)
            => Kind == other.Kind && Source == other.Source && Target == other.Target;

        public override int GetHashCode()
            => HashCode.Combine((int) Kind, Source, Target);
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<VampireDrainBeamEvent>(OnDrainBeamEvent);
        SubscribeNetworkEvent<VampireBloodBondBeamEvent>(OnBloodBondBeamEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeBeamVisuals.Count == 0)
            return;

        foreach (var (key, beamEntity) in _activeBeamVisuals)
        {
            if (!Exists(key.Source) || !Exists(key.Target) || !Exists(beamEntity))
            {
                _removeBuffer.Add(key);
                if (Exists(beamEntity))
                    QueueDel(beamEntity);
                continue;
            }

            UpdateBeamVisual(beamEntity, key.Source, key.Target);
        }

        for (var i = _removeBuffer.Count - 1; i >= 0; i--)
        {
            _activeBeamVisuals.Remove(_removeBuffer[i]);
        }

        _removeBuffer.Clear();
    }

    private void OnDrainBeamEvent(VampireDrainBeamEvent ev)
    {
        HandleBeamEvent(ev.Source, ev.Target, ev.Create, BeamKind.Drain, DrainPrototype);
    }

    private void OnBloodBondBeamEvent(VampireBloodBondBeamEvent ev)
    {
        HandleBeamEvent(ev.Source, ev.Target, ev.Create, BeamKind.BloodBond, BloodBondPrototype);
    }

    private void HandleBeamEvent(NetEntity sourceNet, NetEntity targetNet, bool create, BeamKind kind, string prototype)
    {
        var source = GetEntity(sourceNet);
        var target = GetEntity(targetNet);

        if (!Exists(source) || !Exists(target))
            return;

        var key = new BeamKey(kind, source, target);

        if (create)
        {
            CreateBeamVisual(kind, prototype, source, target);
            return;
        }

        if (_activeBeamVisuals.TryGetValue(key, out var beamEntity))
        {
            QueueDel(beamEntity);
            _activeBeamVisuals.Remove(key);
        }
    }

    private void CreateBeamVisual(BeamKind kind, string prototype, EntityUid source, EntityUid target)
    {
        var key = new BeamKey(kind, source, target);

        if (_activeBeamVisuals.TryGetValue(key, out var existingBeam))
            QueueDel(existingBeam);

        var beam = Spawn(prototype, new EntityCoordinates(source, default));

        _activeBeamVisuals[key] = beam;

        UpdateBeamVisual(beam, source, target);
    }

    private void UpdateBeamVisual(EntityUid beam, EntityUid source, EntityUid target)
    {
        if (!TryComp<SpriteComponent>(beam, out var sprite))
            return;

        var sourcePos = _transform.GetWorldPosition(source);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = targetPos - sourcePos;
        var distance = direction.Length();

        if (distance < 0.1f)
            return;

        var worldAngle = direction.ToWorldAngle() + BeamAngleOffset;

        var midpoint = sourcePos + (direction * 0.5f);
        _transform.SetWorldPosition(beam, midpoint);

        _transform.SetWorldRotation(beam, worldAngle);
        _sprite.SetRotation((beam, sprite), Angle.Zero);

        // Scale beam to match distance. Isvertical ? scale Y : scale X
        var length = MathF.Max(0.05f, distance);
        var thickness = 0.9f;
        var scale = SpriteIsVertical ? new Vector2(thickness, length) : new Vector2(length, thickness);
        _sprite.SetScale((beam, sprite), scale);
        _sprite.SetOffset((beam, sprite), Vector2.Zero);
    }

    public override void Shutdown()
    {
        // Clean up all beam visuals
        foreach (var beamEntity in _activeBeamVisuals.Values)
        {
            if (Exists(beamEntity))
                QueueDel(beamEntity);
        }
        _activeBeamVisuals.Clear();

        base.Shutdown();
    }
}
