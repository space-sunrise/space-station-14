using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Expands semantic visual events into managed particle layers while applying LOD, anchors, and direction context.
/// </summary>
public sealed class ParticleOrchestraSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly ParticleVisualAnchorSystem _anchors = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float MinimumIntensity = 0.01f;
    private const int MaxPendingLayers = 512;

    private readonly List<PendingLayer> _pendingLayers = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleVisualRequestEvent>(OnParticleVisualRequest);
        SubscribeNetworkEvent<ParticleVisualEvent>(OnParticleVisual);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _pendingLayers.Clear();
    }

    private void OnParticleVisualRequest(ref ParticleVisualRequestEvent args)
    {
        if (args.PredictedBy is not { } predictedBy)
            return;

        if (_player.LocalEntity != predictedBy)
            return;

        // Сервер исключает предсказавшего игрока из рассылки,
        // поэтому локальный одноразовый эффект создаётся только при первом replay.
        if (!_timing.IsFirstTimePredicted)
            return;

        EntityUid? source = TerminatingOrDeleted(args.Source)
            ? null
            : args.Source;
        if (!TryGetCoordinates(source, args.Coordinates, out var coordinates))
            return;

        StartVisual(
            args.Orchestra,
            coordinates,
            source,
            args.Target,
            args.Movement,
            args.ColorOverride,
            args.Intensity,
            args.ColorSource,
            args.FallbackColor,
            args.SpawnOffset);
    }

    private void OnParticleVisual(ParticleVisualEvent args)
    {
        StartVisual(
            args.Orchestra,
            args.Coordinates,
            ResolveNetworkEntity(args.Source),
            ResolveNetworkEntity(args.Target),
            args.Movement,
            args.ColorOverride,
            args.Intensity,
            args.ColorSource,
            args.FallbackColor,
            args.SpawnOffset);
    }

    /// <summary>
    /// Starts a configured orchestra using its source entity as the initial position.
    /// </summary>
    public ActiveParticleOrchestra? Start(
        ParticleOrchestraSpecifier specifier,
        EntityUid source,
        EntityUid? target = null,
        Vector2 movement = default,
        ParticleRuntimeOverrides? runtimeOverrides = null,
        Vector2 spawnOffset = default)
    {
        var sourceColor = ResolveColorSource(specifier.ColorSource, source, target) ?? specifier.FallbackColor;
        var colorOverride = ResolveColorOverride(specifier.ColorOverride, sourceColor);
        return Start(
            specifier.Orchestra,
            source,
            target,
            movement,
            colorOverride,
            specifier.Intensity,
            runtimeOverrides,
            spawnOffset + specifier.SpawnOffset);
    }

    /// <summary>
    /// Starts an orchestra using the source entity as its initial position and optional attachment target.
    /// </summary>
    public ActiveParticleOrchestra? Start(
        ProtoId<ParticleOrchestraPrototype> orchestraId,
        EntityUid source,
        EntityUid? target = null,
        Vector2 movement = default,
        Color? colorOverride = null,
        float intensity = 1f,
        ParticleRuntimeOverrides? runtimeOverrides = null,
        Vector2 spawnOffset = default)
    {
        if (TerminatingOrDeleted(source))
            return null;

        return StartAt(
            orchestraId,
            _transform.GetMapCoordinates(source),
            source,
            target,
            movement,
            colorOverride,
            intensity,
            runtimeOverrides,
            spawnOffset);
    }

    /// <summary>
    /// Starts an orchestra at an exact world position while retaining optional source and target context.
    /// </summary>
    public ActiveParticleOrchestra? StartAt(
        ProtoId<ParticleOrchestraPrototype> orchestraId,
        MapCoordinates coordinates,
        EntityUid? source = null,
        EntityUid? target = null,
        Vector2 movement = default,
        Color? colorOverride = null,
        float intensity = 1f,
        ParticleRuntimeOverrides? runtimeOverrides = null,
        Vector2 spawnOffset = default)
    {
        if (!_proto.TryIndex(orchestraId, out var orchestra))
        {
            Log.Error($"Particle orchestra '{orchestraId}' does not exist");
            return null;
        }

        var normalizedIntensity = NormalizeIntensity(intensity);
        if (normalizedIntensity < MinimumIntensity)
            return null;

        var context = new ActiveParticleOrchestraContext(
            coordinates,
            source,
            target,
            movement,
            colorOverride,
            normalizedIntensity,
            runtimeOverrides == null ? null : new ParticleRuntimeOverrides(runtimeOverrides),
            spawnOffset);
        var instance = new ActiveParticleOrchestra(context);

        foreach (var layer in orchestra.Layers)
        {
            if (!CanSpawnLayer(layer, context))
                continue;

            if (layer.Delay <= TimeSpan.Zero)
            {
                SpawnLayer(layer, instance);
                continue;
            }

            if (_pendingLayers.Count < MaxPendingLayers)
                _pendingLayers.Add(new PendingLayer(layer, instance, layer.Delay));
        }

        return instance;
    }

    /// <summary>
    /// Spawns a fire-and-forget orchestra on an entity.
    /// </summary>
    public void Spawn(
        ProtoId<ParticleOrchestraPrototype> orchestraId,
        EntityUid source,
        EntityUid? target = null,
        Vector2 movement = default,
        Color? colorOverride = null,
        float intensity = 1f)
    {
        Start(orchestraId, source, target, movement, colorOverride, intensity);
    }

    /// <summary>
    /// Spawns a fire-and-forget orchestra at exact map coordinates.
    /// </summary>
    public void SpawnAt(
        ProtoId<ParticleOrchestraPrototype> orchestraId,
        MapCoordinates coordinates,
        EntityUid? source = null,
        EntityUid? target = null,
        Vector2 movement = default,
        Color? colorOverride = null,
        float intensity = 1f)
    {
        StartAt(orchestraId, coordinates, source, target, movement, colorOverride, intensity);
    }

    /// <summary>
    /// Stops every active and delayed layer owned by an orchestra instance.
    /// </summary>
    public void Stop(ActiveParticleOrchestra? instance)
    {
        if (instance == null || instance.IsStopped)
            return;

        instance.IsStopped = true;
        for (var index = _pendingLayers.Count - 1; index >= 0; index--)
        {
            if (ReferenceEquals(_pendingLayers[index].Instance, instance))
                _pendingLayers.RemoveAt(index);
        }

        foreach (var active in instance.Emitters)
        {
            _particles.RemoveParticle(active.Emitter);
        }

        instance.Emitters.Clear();
    }

    /// <summary>
    /// Updates the global intensity multiplier of all active and delayed layers.
    /// </summary>
    public void UpdateIntensity(ActiveParticleOrchestra? instance, float intensity)
    {
        if (instance == null || instance.IsStopped)
            return;

        instance.Context.Intensity = NormalizeIntensity(intensity);
        foreach (var active in instance.Emitters)
        {
            ParticleSystem.UpdateIntensity(
                active.Emitter,
                instance.Context.Intensity * Math.Max(0f, active.Layer.Intensity));
        }
    }

    /// <summary>
    /// Updates the global tint of all active and delayed layers.
    /// </summary>
    public void UpdateColor(ActiveParticleOrchestra? instance, Color? colorOverride)
    {
        if (instance == null || instance.IsStopped)
            return;

        instance.Context.ColorOverride = colorOverride;
        foreach (var active in instance.Emitters)
        {
            active.Emitter.ColorOverride = ResolveColorOverride(active.Layer.ColorOverride, colorOverride);
        }
    }

    /// <summary>
    /// Patches runtime overrides on all active layers and retains them for delayed layers.
    /// </summary>
    public void UpdateRuntime(ActiveParticleOrchestra? instance, ParticleRuntimeOverrides runtimeOverrides)
    {
        if (instance == null || instance.IsStopped)
            return;

        instance.Context.RuntimeOverrides ??= new ParticleRuntimeOverrides();
        instance.Context.RuntimeOverrides.Merge(runtimeOverrides);
        foreach (var active in instance.Emitters)
        {
            ParticleSystem.UpdateRuntime(active.Emitter, runtimeOverrides);
        }
    }

    /// <summary>
    /// Moves the common spawn offset while preserving every layer's own offset and semantic anchor.
    /// </summary>
    public void UpdateSpawnOffset(ActiveParticleOrchestra? instance, Vector2 spawnOffset)
    {
        if (instance == null || instance.IsStopped)
            return;

        instance.Context.SpawnOffset = spawnOffset;
        foreach (var active in instance.Emitters)
        {
            UpdateEmitterOffset(active, instance.Context);
        }
    }

    /// <summary>
    /// Points every active and delayed layer toward a world position.
    /// </summary>
    public void UpdateTargetPosition(ActiveParticleOrchestra? instance, Vector2 targetPosition)
    {
        if (instance == null || instance.IsStopped)
            return;

        instance.Context.TargetPosition = targetPosition;
        foreach (var active in instance.Emitters)
        {
            active.Emitter.TargetPosition = targetPosition;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var elapsed = TimeSpan.FromSeconds(frameTime);
        for (var index = _pendingLayers.Count - 1; index >= 0; index--)
        {
            var pending = _pendingLayers[index];
            if (pending.Instance.IsStopped)
            {
                _pendingLayers.RemoveAt(index);
                continue;
            }

            pending.Remaining -= elapsed;
            if (pending.Remaining > TimeSpan.Zero)
                continue;

            SpawnLayer(pending.Layer, pending.Instance);
            _pendingLayers.RemoveAt(index);
        }
    }

    private bool CanSpawnLayer(
        ParticleOrchestraLayerData layer,
        ActiveParticleOrchestraContext context)
    {
        if (!_particles.IsQualityEnabled(layer.MinimumQuality))
            return false;

        if (layer.AllowedFacings == null)
            return true;

        return context.Source is { } source &&
               !TerminatingOrDeleted(source) &&
               layer.AllowedFacings.Contains(_anchors.GetFacingDirection(source));
    }

    private void SpawnLayer(ParticleOrchestraLayerData layer, ActiveParticleOrchestra instance)
    {
        var context = instance.Context;
        if (!CanSpawnLayer(layer, context))
            return;

        var intensity = context.Intensity * Math.Max(0f, layer.Intensity);
        if (intensity < MinimumIntensity)
            return;

        var sourceColor = ResolveColorSource(layer.ColorSource, context.Source, context.Target);

        sourceColor ??= layer.FallbackColor;
        var layerColor = ResolveColorOverride(layer.ColorOverride, sourceColor);
        var colorOverride = ResolveColorOverride(layerColor, context.ColorOverride);
        var overrides = CreateRuntimeOverrides(layer, context);
        var layerOffset = context.SpawnOffset + layer.Offset;
        if (layer.FillSourceSprite &&
            context.Source is { } boundsSource &&
            !TerminatingOrDeleted(boundsSource) &&
            _anchors.TryGetEmissionBox(
                boundsSource,
                Vector2.Zero,
                layer.SourceSpriteCoverage,
                out var boundsOffset,
                out var boundsHalfExtents))
        {
            overrides ??= new ParticleRuntimeOverrides();
            overrides.EmissionShape = EmissionShapeType.Box;
            overrides.EmissionBoxExtents = boundsHalfExtents;
            layerOffset += boundsOffset;
        }

        Vector2? initialVelocity = context.Movement.LengthSquared() > 0.0001f
            ? context.Movement
            : null;
        ActiveEmitter? emitter;

        if (context.Source is not { } sourceEntity || TerminatingOrDeleted(sourceEntity))
        {
            var fallbackOffset = layerOffset;
            if (layer.Anchor is { } fallbackAnchor)
                fallbackOffset += _anchors.GetFallbackOffset(fallbackAnchor, layer.LateralOffset);

            emitter = _particles.CreateParticle(
                layer.Effect,
                context.Coordinates,
                colorOverride,
                overrides,
                initialVelocity: initialVelocity,
                intensity: intensity,
                spawnOffset: fallbackOffset);
        }
        else if (layer.Anchor is { } anchor && layer.Attach)
        {
            emitter = _particles.CreateParticleAnchored(
                layer.Effect,
                sourceEntity,
                anchor,
                colorOverride,
                overrides,
                initialVelocity,
                intensity: intensity,
                anchorOffset: layerOffset,
                lateralOffset: layer.LateralOffset);
        }
        else
        {
            var spawnOffset = layerOffset;
            var spawnCoordinates = context.Coordinates;
            if (layer.Anchor is { } detachedAnchor)
            {
                spawnOffset += _anchors.GetOffset(sourceEntity, detachedAnchor, layer.LateralOffset);
                spawnCoordinates = _transform.GetMapCoordinates(sourceEntity);
            }

            emitter = layer.Attach
                ? _particles.CreateParticle(
                    layer.Effect,
                    sourceEntity,
                    colorOverride,
                    overrides: overrides,
                    initialVelocity: initialVelocity,
                    intensity: intensity,
                    spawnOffset: spawnOffset)
                : _particles.CreateParticle(
                    layer.Effect,
                    spawnCoordinates,
                    colorOverride,
                    overrides,
                    initialVelocity,
                    intensity: intensity,
                    spawnOffset: spawnOffset);
        }

        if (emitter != null)
        {
            emitter.TargetPosition = context.TargetPosition;
            instance.Emitters.Add(new ActiveParticleOrchestraEmitter(emitter, layer));
        }
    }

    private ParticleRuntimeOverrides? CreateRuntimeOverrides(
        ParticleOrchestraLayerData layer,
        ActiveParticleOrchestraContext context)
    {
        var direction = ResolveDirection(layer.Direction, context);
        if (direction.LengthSquared() <= 0.0001f && layer.SpreadAngle == null)
        {
            return context.RuntimeOverrides == null
                ? null
                : new ParticleRuntimeOverrides(context.RuntimeOverrides);
        }

        var overrides = context.RuntimeOverrides == null
            ? new ParticleRuntimeOverrides()
            : new ParticleRuntimeOverrides(context.RuntimeOverrides);
        if (direction.LengthSquared() > 0.0001f)
            overrides.EmitAngle = _anchors.GetEmitAngle(direction);
        if (layer.SpreadAngle is { } spreadAngle)
            overrides.SpreadAngle = spreadAngle;
        return overrides;
    }

    private void UpdateEmitterOffset(
        ActiveParticleOrchestraEmitter active,
        ActiveParticleOrchestraContext context)
    {
        var layer = active.Layer;
        var emitter = active.Emitter;
        var baseOffset = context.SpawnOffset + layer.Offset;

        if (context.Source is not { } source || TerminatingOrDeleted(source))
        {
            emitter.SpawnOffset = layer.Anchor is { } fallbackAnchor
                ? baseOffset + _anchors.GetFallbackOffset(fallbackAnchor, layer.LateralOffset)
                : baseOffset;
            return;
        }

        if (layer.FillSourceSprite &&
            _anchors.TryGetEmissionBox(
                source,
                Vector2.Zero,
                layer.SourceSpriteCoverage,
                out var boundsOffset,
                out var boundsHalfExtents))
        {
            baseOffset += boundsOffset;
            emitter.Overrides ??= new ParticleRuntimeOverrides();
            emitter.Overrides.EmissionShape = EmissionShapeType.Box;
            emitter.Overrides.EmissionBoxExtents = boundsHalfExtents;
        }

        if (layer.Anchor is { } anchor && layer.Attach)
        {
            emitter.VisualAnchorOffset = baseOffset;
            emitter.SpawnOffset = _anchors.GetOffset(source, anchor, layer.LateralOffset) + baseOffset;
            return;
        }

        emitter.SpawnOffset = layer.Anchor is { } detachedAnchor
            ? baseOffset + _anchors.GetOffset(source, detachedAnchor, layer.LateralOffset)
            : baseOffset;
    }

    private Vector2 ResolveDirection(
        ParticleOrchestraDirection direction,
        ActiveParticleOrchestraContext context)
    {
        return direction switch
        {
            ParticleOrchestraDirection.Movement => context.Movement,
            ParticleOrchestraDirection.OppositeMovement => -context.Movement,
            ParticleOrchestraDirection.SourceToPosition => GetDirection(context.Source, context.Coordinates),
            ParticleOrchestraDirection.PositionToSource => -GetDirection(context.Source, context.Coordinates),
            ParticleOrchestraDirection.SourceToTarget => GetDirection(context.Source, context.Target),
            ParticleOrchestraDirection.TargetToSource => GetDirection(context.Target, context.Source),
            ParticleOrchestraDirection.SourceFacing => GetFacingDirection(context.Source),
            _ => Vector2.Zero,
        };
    }

    private Vector2 GetDirection(EntityUid? source, MapCoordinates targetCoordinates)
    {
        if (!TryGetMapCoordinates(source, out var sourceCoordinates))
            return Vector2.Zero;

        if (sourceCoordinates.MapId != targetCoordinates.MapId)
            return Vector2.Zero;

        return targetCoordinates.Position - sourceCoordinates.Position;
    }

    private Vector2 GetDirection(EntityUid? source, EntityUid? target)
    {
        if (!TryGetMapCoordinates(source, out var sourceCoordinates))
            return Vector2.Zero;

        if (!TryGetMapCoordinates(target, out var targetCoordinates))
            return Vector2.Zero;

        if (sourceCoordinates.MapId != targetCoordinates.MapId)
            return Vector2.Zero;

        return targetCoordinates.Position - sourceCoordinates.Position;
    }

    private Vector2 GetFacingDirection(EntityUid? source)
    {
        if (source is not { } sourceEntity || TerminatingOrDeleted(sourceEntity))
            return Vector2.Zero;

        return _anchors.GetFacingWorldDirection(sourceEntity);
    }

    private bool TryGetMapCoordinates(EntityUid? entity, out MapCoordinates coordinates)
    {
        coordinates = default;
        if (entity is not { } uid || TerminatingOrDeleted(uid))
            return false;

        coordinates = _transform.GetMapCoordinates(uid);
        return true;
    }

    private EntityUid? ResolveNetworkEntity(NetEntity? networkEntity)
    {
        if (networkEntity is not { } netEntity)
            return null;

        var entity = GetEntity(netEntity);
        return entity.IsValid() && !TerminatingOrDeleted(entity)
            ? entity
            : null;
    }

    private bool TryGetCoordinates(
        EntityUid? source,
        MapCoordinates? fallback,
        out MapCoordinates coordinates)
    {
        if (fallback is { } exactCoordinates)
        {
            coordinates = exactCoordinates;
            return true;
        }

        coordinates = default;
        if (source is not { } sourceEntity || TerminatingOrDeleted(sourceEntity))
            return false;

        coordinates = _transform.GetMapCoordinates(sourceEntity);
        return true;
    }

    private void StartVisual(
        ProtoId<ParticleOrchestraPrototype> orchestra,
        MapCoordinates coordinates,
        EntityUid? source,
        EntityUid? target,
        Vector2 movement,
        Color? colorOverride,
        float intensity,
        ParticleVisualColorSource colorSource,
        Color? fallbackColor,
        Vector2 spawnOffset)
    {
        var sourceColor = ResolveColorSource(colorSource, source, target);
        sourceColor ??= fallbackColor;
        StartAt(
            orchestra,
            coordinates,
            source,
            target,
            movement,
            ResolveColorOverride(colorOverride, sourceColor),
            intensity,
            spawnOffset: spawnOffset);
    }

    private Color? ResolveColorSource(
        ParticleVisualColorSource colorSource,
        EntityUid? source,
        EntityUid? target)
    {
        switch (colorSource)
        {
            case ParticleVisualColorSource.SourcePointLight:
                return source is { } lightSource &&
                       !TerminatingOrDeleted(lightSource) &&
                       TryComp<PointLightComponent>(lightSource, out var light)
                    ? ParticleColorHelper.EnsureVisible(light.Color)
                    : null;
            case ParticleVisualColorSource.TargetSpriteDominant:
                return target is { } spriteTarget &&
                       !TerminatingOrDeleted(spriteTarget) &&
                       _anchors.TryGetDominantSpriteColor(spriteTarget, out var dominantColor)
                    ? dominantColor
                    : null;
            default:
                return null;
        }
    }

    private static Color? ResolveColorOverride(Color? layerColor, Color? contextColor)
    {
        if (layerColor is not { } layer)
            return contextColor;

        if (contextColor is not { } context)
            return layer;

        return new Color(
            layer.R * context.R,
            layer.G * context.G,
            layer.B * context.B,
            layer.A * context.A);
    }

    private static float NormalizeIntensity(float intensity)
        => float.IsFinite(intensity)
            ? Math.Max(0f, intensity)
            : 0f;

    private sealed class PendingLayer(
        ParticleOrchestraLayerData layer,
        ActiveParticleOrchestra instance,
        TimeSpan remaining)
    {
        public readonly ParticleOrchestraLayerData Layer = layer;
        public readonly ActiveParticleOrchestra Instance = instance;
        public TimeSpan Remaining = remaining;
    }
}
