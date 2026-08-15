using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Manages active particle emitters on the client, including their simulation and rendering via <see cref="ParticleOverlay"/>.
/// </summary>
public sealed partial class ParticleSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly ParticleVisualAnchorSystem _anchors = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>Maximum number of sub-emitter chains allowed. Prevents infinite recursive sub-emitter chains.</summary>
    public const int MaxSubEmitterDepth = 3;
    internal const float EmitterCullMargin = 4f;

    /// <summary>
    /// Absolute ceiling on live particles regardless of quality settings or anything else.
    /// Isolated testing reached roughly 26,000 simultaneous particles before significant frame drops.
    /// This number is NOT a target and MUST NOT be treated as one and is intentionally set well below that for several reasons:
    /// <list type="bullet">
    ///   <item><b>All particle simulation runs entirely on the CPU</b>. Every particle competes
    ///   with gameplay logic, physics, networking, and rendering on the same thread.</item>
    ///   <item>That 26k figure was measured in isolation. In a real round with entities, atmos, and players,
    ///   performance will degrade significantly sooner.</item>
    ///   <item>Emitters stack multiplicatively. Ten "small" effects at 500 particles each is already
    ///   5,000 particles before considering anything else in the scene.</item>
    /// </list>
    /// <b>Do not raise this limit just because your machine can handle it.</b>
    /// This limit exists to protect performance across all hardware and real gameplay conditions.
    /// If you believe this needs to be increased, you should first justify why the effect cannot
    /// be achieved more efficiently.
    /// </summary>
    private const int HardMaxParticles = 8000;
    private const int HardMaxEmitters = 512;

    /// <summary>
    /// Maximum particles per emitter for <see cref="ParticleEffectPrototype.IgnoreQualitySettings"/> effects
    /// when quality is below High. At High quality they respect the full <see cref="HardMaxParticles"/> ceiling.
    /// </summary>
    private const int IgnoreQualityMaxParticles = 64;

    /// <summary>Maximum supported intensity multiplier.</summary>
    private const float MaxIntensity = 8f;

    /// <summary>
    /// Reserved budget for gameplay-critical effects when cosmetic particles are disabled.
    /// </summary>
    private const int IgnoreQualityMinimumBudget = 512;

    // Множители количества для уровней Off, Low, Medium и High.
    private static readonly float[] QualityMultipliers = { 0f, 0.25f, 0.5f, 1f };

    // Стандартные глобальные бюджеты для каждого уровня качества.
    private static readonly int[] QualityBudgets = { 0, 2250, 5500, 8000 };

    private readonly List<ActiveEmitter> _emitters = new();
    private readonly List<(ProtoId<ParticleEffectPrototype> Id, MapCoordinates Coords, int Depth)> _pendingSubEmitters = new();
    private readonly Stack<ParticleData> _particlePool = new(HardMaxParticles);

    // Кэш кадров не позволяет повторно разрешать один RSI для каждого нового эмиттера.
    private readonly Dictionary<string, (Texture[] Frames, float[] Delays)> _frameCache = new();
    private readonly Dictionary<string, CompiledParticleEffect> _compiledEffects = new();
    private readonly HashSet<string> _frameResolveFailures = new();

    private ParticleOverlay _overlay = default!;
    private int _liveParticleCount;
    private int _quality;
    private int _globalBudget;
    private int _configuredGlobalBudget = HardMaxParticles;
    private uint _nextHandle = 1;

    #region =^..^= Particle System API =^..^=
    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ParticleOverlay(this);
        _overlayManager.AddOverlay(_overlay);

        _cfg.OnValueChanged(CCVars.ParticleQuality, OnQualityChanged, invokeImmediately: true);
        // Именованный обработчик можно симметрично отписать при отключении клиента.
        _cfg.OnValueChanged(CCVars.ParticleGlobalBudget, OnGlobalBudgetChanged, invokeImmediately: true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.ParticleQuality, OnQualityChanged);
        // Не оставляем подписку и кэши между клиентскими сессиями.
        _cfg.UnsubValueChanged(CCVars.ParticleGlobalBudget, OnGlobalBudgetChanged);
        _overlayManager.RemoveOverlay(_overlay);
        ReleaseAllEmitters();
        _pendingSubEmitters.Clear();
        _particlePool.Clear();
        _frameCache.Clear();
        _compiledEffects.Clear();
        _frameResolveFailures.Clear();
        _liveParticleCount = 0;
    }

    private void OnQualityChanged(int quality)
    {
        // Low и Medium не должны перезаписываться высоким пользовательским бюджетом.
        var oldQuality = _quality;
        _quality = Math.Clamp(quality, 0, QualityBudgets.Length - 1);
        RecalculateGlobalBudget();

        if (oldQuality != 0 && _quality == 0)
            KillCosmeticParticles();
    }

    // Единая нормализация бюджета для обеих CVar.
    private void OnGlobalBudgetChanged(int budget)
    {
        _configuredGlobalBudget = Math.Clamp(budget, 0, HardMaxParticles);
        RecalculateGlobalBudget();
    }

    private void RecalculateGlobalBudget()
    {
        _globalBudget = _quality == QualityBudgets.Length - 1
            ? _configuredGlobalBudget
            : Math.Min(_configuredGlobalBudget, QualityBudgets[_quality]);
    }

    private int GetGlobalBudget(ParticleEffectPrototype proto)
    {
        if (proto.IgnoreQualitySettings || proto.Priority == ParticleEffectPriority.Critical)
            return Math.Max(_globalBudget, IgnoreQualityMinimumBudget);

        var fraction = proto.Priority switch
        {
            ParticleEffectPriority.Decorative => 0.7f,
            ParticleEffectPriority.Ambient => 0.85f,
            ParticleEffectPriority.Interaction => 0.95f,
            _ => 1f,
        };
        return (int) MathF.Floor(_globalBudget * fraction);
    }

    public IReadOnlyList<ActiveEmitter> GetEmitters() => _emitters;

    /// <summary>Current user-selected particle quality.</summary>
    public ParticleQualityLevel Quality => (ParticleQualityLevel) _quality;

    /// <summary>Returns whether a prototype-authored LOD layer is enabled at the current quality.</summary>
    public bool IsQualityEnabled(ParticleQualityLevel minimumQuality)
        => _quality >= (int) minimumQuality;

    /// <summary>
    /// Immediately destroys all active emitters and kills every live particle.
    /// Use this only when immediate recovery from a malformed or unexpectedly expensive effect is required.
    /// </summary>
    /// <returns>Number of emitters that were cleared.</returns>
    public int KillAll()
    {
        var count = _emitters.Count;
        ReleaseAllEmitters();
        _pendingSubEmitters.Clear();
        _liveParticleCount = 0;
        return count;
    }

    /// <summary>Spawns a particle effect at a given map coordinate.</summary>
    public ActiveEmitter? SpawnEffect(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        EntityUid? attachedEntity = null,
        Color? colorOverride = null,
        ParticleRuntimeOverrides? overrides = null,
        Vector2? initialVelocity = null,
        float intensity = 1f,
        Vector2? spawnOffset = null,
        ParticleVisualAnchor? visualAnchor = null,
        float visualAnchorLateralOffset = 0f)
    {
        return SpawnEffect(
            effectId,
            coords,
            depth: 0,
            attachedEntity,
            colorOverride,
            overrides,
            initialVelocity,
            intensity,
            spawnOffset,
            visualAnchor,
            visualAnchorLateralOffset);
    }

    private ActiveEmitter? SpawnEffect(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        int depth,
        EntityUid? attachedEntity = null,
        Color? colorOverride = null,
        ParticleRuntimeOverrides? overrides = null,
        Vector2? initialVelocity = null,
        float intensity = 1f,
        Vector2? spawnOffset = null,
        ParticleVisualAnchor? visualAnchor = null,
        float visualAnchorLateralOffset = 0f)
    {
        if (depth > MaxSubEmitterDepth)
        {
            Log.Warning($"ParticleSystem: sub-emitter depth exceeded MaxSubEmitterDepth ({MaxSubEmitterDepth}). Dropping '{effectId}'.");
            return null;
        }

        if (!_protoManager.TryIndex(effectId, out var proto))
            return null;

        // Постоянный эмиттер сохраняется в спящем состоянии и возобновится после включения качества.
        if (GetEffectivePriority(proto) != ParticleEffectPriority.Critical &&
            !IsQualityEnabled(proto.MinimumQuality) &&
            !IsPersistentEmitter(proto, overrides))
            return null;

        // Even IgnoreQualitySettings effects are capped at 8 simultaneous emitters when quality is Off.
        if (_quality == 0 && GetEffectivePriority(proto) == ParticleEffectPriority.Critical)
        {
            var criticalEmitterCount = 0;
            foreach (var e in _emitters)
            {
                if (GetEffectivePriority(e.Proto) == ParticleEffectPriority.Critical)
                    criticalEmitterCount++;
            }
            if (criticalEmitterCount >= 8)
                return null;
        }

        var emitter = CreateEmitter(proto, coords, attachedEntity);
        if (emitter.Frames.Length == 0)
            return null;

        if (!TryAdmitEmitter(GetEffectivePriority(proto)))
            return null;

        emitter.ColorOverride = colorOverride;
        emitter.SubEmitterDepth = depth;
        emitter.Intensity = NormalizeIntensity(intensity);

        if (spawnOffset.HasValue)
            emitter.SpawnOffset = spawnOffset.Value;

        if (overrides != null)
            ApplyOverrides(emitter, overrides);

        if (visualAnchor.HasValue && attachedEntity.HasValue)
        {
            emitter.VisualAnchor = visualAnchor;
            emitter.VisualAnchorOffset = emitter.Overrides?.SpawnOffset ?? spawnOffset ?? proto.SpawnOffset;
            emitter.VisualAnchorLateralOffset = visualAnchorLateralOffset;
            emitter.SpawnOffset = _anchors.GetOffset(
                    attachedEntity.Value,
                    visualAnchor.Value,
                    visualAnchorLateralOffset) +
                emitter.VisualAnchorOffset;
        }

        // Pre-seed velocity so burst emitters can use InheritVelocity correctly.
        if (initialVelocity.HasValue)
        {
            emitter.EmitterVelocity = initialVelocity.Value;
            emitter.PreviousPosition = coords.Position;
            emitter.PreviousMapId = coords.MapId;
            emitter.VelocityInitialized = true;
        }

        // Add before BurstEmit so the live count is tracked correctly when EmitParticle runs.
        _emitters.Add(emitter);

        if (proto.Burst)
        {
            BurstEmit(emitter);
            emitter.Exhausted = true;
        }

        return emitter;
    }

    /// <summary>
    /// Patches runtime overrides on a live emitter by handle.
    /// Only non-null fields are applied, null fields are left unchanged.
    /// </summary>
    public void UpdateRuntime(uint handle, ParticleRuntimeOverrides overrides)
    {
        if (handle == 0)
            return;
        foreach (var emitter in _emitters)
        {
            if (emitter.Handle == handle)
            {
                ApplyOverrides(emitter, overrides);
                break;
            }
        }
    }

    /// <summary>
    /// Patches runtime overrides on a live emitter by direct reference.
    /// Use this when you already have the <see cref="ActiveEmitter"/> from <see cref="SpawnEffect"/>.
    /// </summary>
    public static void UpdateRuntime(ActiveEmitter emitter, ParticleRuntimeOverrides overrides)
        => ApplyOverrides(emitter, overrides);

    private static void ApplyOverrides(ActiveEmitter emitter, ParticleRuntimeOverrides src)
    {
        emitter.Overrides ??= new ParticleRuntimeOverrides();
        var dst = emitter.Overrides;

        dst.Merge(src);

        if (emitter.VisualAnchor.HasValue && src.SpawnOffset is { } anchoredOffset)
            emitter.VisualAnchorOffset = anchoredOffset;

        if (src.EmitAngle is { } emitAngle)
        {
            dst.EmitAngle = emitAngle;
            if (emitter.TargetEntity == null && emitter.TargetPosition == null)
                emitter.EffectiveEmitAngle = (float)emitAngle.Theta;
        }
    }

    /// <summary>
    /// Spawns a particle effect whose emission direction tracks a target entity each tick.
    /// When the entity is deleted the emitter retains its last angle.
    /// </summary>
    public ActiveEmitter? SpawnEffectAimAt(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        EntityUid targetEntity,
        EntityUid? attachedEntity = null)
    {
        var emitter = SpawnEffect(effectId, coords, attachedEntity);
        if (emitter != null)
            emitter.TargetEntity = targetEntity;
        return emitter;
    }

    /// <summary>
    /// Spawns a particle effect whose emission direction points at a fixed world position.
    /// </summary>
    public ActiveEmitter? SpawnEffectAimAt(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        Vector2 targetWorldPosition,
        EntityUid? attachedEntity = null)
    {
        var emitter = SpawnEffect(effectId, coords, attachedEntity);
        if (emitter != null)
            emitter.TargetPosition = targetWorldPosition;
        return emitter;
    }

    public override void FrameUpdate(float frameTime)
    {
        var eye = _eye.CurrentEye;
        var eyePos = eye.Position.Position;
        var eyeAngle = (float)eye.Rotation;
        var halfSize = new Vector2(eye.Zoom.X > 0 ? 20f / eye.Zoom.X : 20f, eye.Zoom.Y > 0 ? 15f / eye.Zoom.Y : 15f) * 1.5f;
        var viewBounds = new Box2(eyePos - halfSize, eyePos + halfSize).Enlarged(EmitterCullMargin);
        var currentMapId = eye.Position.MapId;

        // Iterate emitters in reverse so we can safely remove exhausted ones by index.
        // For each emitter: skip full simulation if off-screen (only age particles), otherwise tick it.
        // Remove any emitter that is exhausted and has no live particles left.
        for (var i = _emitters.Count - 1; i >= 0; i--)
        {
            var emitter = _emitters[i];

            // Позиция и скорость привязанного эмиттера обновляются и вне экрана.
            UpdateEmitterTransform(emitter, frameTime);

            var inView = emitter.MapCoords.MapId == currentMapId
                && viewBounds.Contains(emitter.MapCoords.Position);

            if (inView)
                TickEmitter(emitter, frameTime, eyeAngle);
            else
                AgeOffScreenParticles(emitter, frameTime);

            if (emitter.Exhausted && emitter.Particles.Count == 0)
                RecycleEmitterAt(i);
        }

        // Spawn any sub-emitters collected during this tick.
        // Use an index-based while loop instead of foreach because SpawnEffect can itself add
        // new entries to _pendingSubEmitters (nested sub-emitters), which would
        // throw if we were iterating with an enumerator.
        var subIdx = 0;
        while (subIdx < _pendingSubEmitters.Count)
        {
            var (id, coords, depth) = _pendingSubEmitters[subIdx];
            subIdx++;
            SpawnEffect(id, coords, depth: depth);
        }
        _pendingSubEmitters.Clear();
    }

    #endregion

    #region =^..^= Emitter Internals =^..^=

    private ActiveEmitter CreateEmitter(ParticleEffectPrototype proto, MapCoordinates coords, EntityUid? attached)
    {
        var emitter = new ActiveEmitter
        {
            Proto = proto,
            Compiled = GetCompiledEffect(proto),
            MapCoords = coords,
            AttachedEntity = attached,
            Handle = _nextHandle++,
            SpawnOffset = proto.SpawnOffset,
        };
        ResolveFrames(emitter);

        emitter.EffectiveEmitAngle = (float)emitter.Proto.EmitAngle.Theta;

        foreach (var _ in proto.Bursts)
            emitter.FiredBursts.Add(false);

        return emitter;
    }

    private CompiledParticleEffect GetCompiledEffect(ParticleEffectPrototype proto)
    {
        if (_compiledEffects.TryGetValue(proto.ID, out var compiled))
            return compiled;

        compiled = new CompiledParticleEffect(proto);
        _compiledEffects.Add(proto.ID, compiled);
        return compiled;
    }

    /// <summary>Stops a running emitter, preventing new particles from being emitted. Existing particles live out their lifetime.</summary>
    public void StopEffect(uint handle)
    {
        if (handle == 0)
            return;
        foreach (var emitter in _emitters)
        {
            if (emitter.Handle == handle)
            {
                emitter.Exhausted = true;
                break;
            }
        }
    }

    /// <summary>Stops a running emitter by direct reference. Existing particles live out their lifetime.</summary>
    public static void StopEffect(ActiveEmitter emitter) => emitter.Exhausted = true;

    /// <summary>Updates the intensity multiplier on a running emitter by handle.</summary>
    public void UpdateIntensity(uint handle, float intensity)
    {
        if (handle == 0)
            return;
        foreach (var emitter in _emitters)
        {
            if (emitter.Handle == handle)
            {
                emitter.Intensity = NormalizeIntensity(intensity);
                break;
            }
        }
    }

    /// <summary>Updates the intensity multiplier on a running emitter by direct reference.</summary>
    public static void UpdateIntensity(ActiveEmitter emitter, float intensity)
        => emitter.Intensity = NormalizeIntensity(intensity);

    #endregion
}
