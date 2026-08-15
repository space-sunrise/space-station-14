using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Temperature.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Supplies fire intensity and sprite-sized emission geometry to managed particle orchestras.
/// </summary>
public sealed class FlammableParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly ParticleVisualAnchorSystem _anchors = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleOrchestraPrototype> DefaultOrchestra = "FireAmbient";

    private const float MaxStacks = 10f;
    private const float AlwaysHotIntensity = 1.25f;
    private const float FireSourceCoverage = 0.85f;

    private readonly Dictionary<EntityUid, FireState> _active = [];
    private readonly List<EntityUid> _stale = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlammableComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<FlammableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AlwaysHotComponent, ComponentInit>(OnAlwaysHotInit);
        SubscribeLocalEvent<AlwaysHotComponent, ComponentShutdown>(OnAlwaysHotShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var state in _active.Values)
        {
            StopState(state);
        }

        _active.Clear();
        _stale.Clear();
    }

    private void OnAppearanceChange(Entity<FlammableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent, FireVisuals.OnFire, out bool onFire))
            onFire = false;

        if (!_appearance.TryGetData(ent, FireVisuals.FireStacks, out float stacks))
            stacks = 0f;

        if (!onFire)
        {
            if (HasComp<AlwaysHotComponent>(ent))
                return;

            if (_active.Remove(ent, out var extinguished))
                StopState(extinguished);

            return;
        }

        var state = EnsureState(ent);
        var intensity = Math.Clamp(stacks / MaxStacks * 2f, 1f, 2f);
        _orchestra.UpdateIntensity(state.Orchestra, intensity);
    }

    private void OnShutdown(Entity<FlammableComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<AlwaysHotComponent>(ent))
            return;

        if (_active.Remove(ent, out var state))
            StopState(state);
    }

    private void OnAlwaysHotInit(Entity<AlwaysHotComponent> ent, ref ComponentInit args)
    {
        var state = EnsureState(ent);
        _orchestra.UpdateIntensity(state.Orchestra, AlwaysHotIntensity);
    }

    private void OnAlwaysHotShutdown(Entity<AlwaysHotComponent> ent, ref ComponentShutdown args)
    {
        if (_appearance.TryGetData(ent, FireVisuals.OnFire, out bool onFire) && onFire)
            return;

        if (_active.Remove(ent, out var state))
            StopState(state);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _stale.Clear();
        foreach (var (uid, state) in _active)
        {
            if (TerminatingOrDeleted(uid))
            {
                _stale.Add(uid);
                continue;
            }

            UpdateGeometry(uid, state);
        }

        foreach (var uid in _stale)
        {
            StopState(_active[uid]);
            _active.Remove(uid);
        }
    }

    private FireState EnsureState(EntityUid uid)
    {
        if (_active.TryGetValue(uid, out var state))
            return state;

        var coordinates = _transform.GetMapCoordinates(uid);
        TryComp<FireParticleVisualsComponent>(uid, out var visuals);
        var orchestraId = visuals?.Orchestra ?? DefaultOrchestra;
        var localOffset = visuals?.Offset ?? Vector2.Zero;
        var fillSprite = visuals?.FillSprite ?? true;
        var spawnOffset = Vector2.Zero;
        ParticleRuntimeOverrides? geometryOverrides = null;

        if (fillSprite &&
            _anchors.TryGetEmissionBox(
                uid,
                localOffset,
                FireSourceCoverage,
                out var spriteOffset,
                out var halfExtents))
        {
            spawnOffset = spriteOffset;
            geometryOverrides = CreateBoxOverrides(halfExtents);
        }
        else if (visuals != null)
        {
            spawnOffset = _anchors.TransformLocalOffset(uid, localOffset);
        }

        state = new FireState(
            _orchestra.StartAt(
                orchestraId,
                coordinates,
                uid,
                runtimeOverrides: geometryOverrides,
                spawnOffset: spawnOffset),
            localOffset,
            fillSprite,
            geometryOverrides);
        _active[uid] = state;
        return state;
    }

    private void UpdateGeometry(EntityUid uid, FireState state)
    {
        if (state.FillSprite &&
            _anchors.TryGetEmissionBox(
                uid,
                state.LocalOffset,
                FireSourceCoverage,
                out var spriteOffset,
                out var halfExtents))
        {
            _orchestra.UpdateSpawnOffset(state.Orchestra, spriteOffset);
            state.GeometryOverrides ??= CreateBoxOverrides(halfExtents);
            state.GeometryOverrides.EmissionBoxExtents = halfExtents;
            _orchestra.UpdateRuntime(state.Orchestra, state.GeometryOverrides);
            return;
        }

        _orchestra.UpdateSpawnOffset(
            state.Orchestra,
            _anchors.TransformLocalOffset(uid, state.LocalOffset));
    }

    private static ParticleRuntimeOverrides CreateBoxOverrides(Vector2 halfExtents)
    {
        return new ParticleRuntimeOverrides
        {
            EmissionShape = EmissionShapeType.Box,
            EmissionBoxExtents = halfExtents,
        };
    }

    private void StopState(FireState state)
    {
        _orchestra.Stop(state.Orchestra);
    }

    private sealed class FireState(
        ActiveParticleOrchestra? orchestra,
        Vector2 localOffset,
        bool fillSprite,
        ParticleRuntimeOverrides? geometryOverrides)
    {
        public readonly ActiveParticleOrchestra? Orchestra = orchestra;
        public readonly Vector2 LocalOffset = localOffset;
        public readonly bool FillSprite = fillSprite;
        public ParticleRuntimeOverrides? GeometryOverrides = geometryOverrides;
    }
}
