using System.Numerics;
using Content.Client.Toggleable;
using Content.Shared._Sunrise.Particles;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Keeps prototype-configured particle orchestras synchronized with item toggle state.
/// </summary>
public sealed class ToggleParticleVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly ParticleVisualAnchorSystem _anchors = default!;

    private const int MaxLayerSamples = 8;
    private const float FlameTipLift = 0.09375f;
    private const string GeneratedToggleLayerMarker = "-toggle";

    private readonly Dictionary<EntityUid, ActiveParticleOrchestra> _active = [];
    private readonly Dictionary<EntityUid, HeldParticleVisual> _heldVisuals = [];
    private readonly List<EntityUid> _heldVisualsToRefresh = [];
    private readonly List<string> _heldLayerKeys = [];
    private readonly List<Vector2> _layerSamples = new(MaxLayerSamples);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleParticleVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ToggleParticleVisualsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ToggleParticleVisualsComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ToggleParticleVisualsComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<ToggleParticleVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged,
            after: [typeof(ToggleableVisualsSystem), typeof(ToggleableLightVisualsSystem)]);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var orchestra in _active.Values)
        {
            _orchestra.Stop(orchestra);
        }

        _active.Clear();
        _heldVisuals.Clear();
        _heldVisualsToRefresh.Clear();
        _heldLayerKeys.Clear();
        _layerSamples.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _heldVisualsToRefresh.Clear();
        foreach (var (item, heldVisual) in _heldVisuals)
        {
            if (TerminatingOrDeleted(item) ||
                TerminatingOrDeleted(heldVisual.Holder) ||
                !_hands.IsHolding(heldVisual.Holder, item))
            {
                _heldVisualsToRefresh.Add(item);
                continue;
            }

            if (!TryComp<SpriteComponent>(heldVisual.Holder, out var sprite) ||
                _anchors.GetFacingDirection(heldVisual.Holder) != heldVisual.Facing ||
                sprite.Rotation != heldVisual.SpriteRotation ||
                sprite.Offset != heldVisual.SpriteOffset)
            {
                _heldVisualsToRefresh.Add(item);
            }
        }

        foreach (var item in _heldVisualsToRefresh)
        {
            if (!_heldVisuals.TryGetValue(item, out var heldVisual))
                continue;

            if (!TryRefreshHeldVisual(item, heldVisual, out var refreshed))
            {
                _heldVisuals.Remove(item);
                Stop(item);

                if (TryComp<ToggleParticleVisualsComponent>(item, out var visuals) &&
                    TryComp<ItemToggleComponent>(item, out var toggle) &&
                    toggle.Activated)
                {
                    SetActive(item, visuals, true);
                }

                continue;
            }

            _heldVisuals[item] = refreshed;
            if (_active.TryGetValue(item, out var orchestra) &&
                orchestra.Context.Source == refreshed.Holder)
            {
                _orchestra.UpdateSpawnOffset(orchestra, refreshed.SpawnOffset);
            }
        }

        // Лежащий предмет может продолжать вращаться после выпадения из руки.
        // Пересчитываем точку пламени, чтобы эмиттер оставался на конце спрайта.
        foreach (var (item, orchestra) in _active)
        {
            if (_heldVisuals.ContainsKey(item) ||
                orchestra.Context.Source != item ||
                !TryComp<ToggleParticleVisualsComponent>(item, out var visuals))
            {
                continue;
            }

            _orchestra.UpdateSpawnOffset(orchestra, GetGroundOffset(item, visuals));
        }
    }

    private void OnStartup(Entity<ToggleParticleVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<ItemToggleComponent>(ent, out var toggle))
            return;

        SetActive(ent, ent.Comp, toggle.Activated);
    }

    private void OnShutdown(Entity<ToggleParticleVisualsComponent> ent, ref ComponentShutdown args)
    {
        Stop(ent);
        _heldVisuals.Remove(ent);
    }

    private void OnToggled(Entity<ToggleParticleVisualsComponent> ent, ref ItemToggledEvent args)
    {
        SetActive(ent, ent.Comp, args.Activated);
    }

    private void OnHeldVisualsUpdated(Entity<ToggleParticleVisualsComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        if (TryGetHeldVisual(args.User, ent, args.RevealedLayers, out var heldVisual))
            _heldVisuals[ent] = heldVisual;
        else
            _heldVisuals.Remove(ent);

        if (!TryComp<ItemToggleComponent>(ent, out var toggle) || !toggle.Activated)
            return;

        SetActive(ent, ent.Comp, true);
    }

    private void OnAppearanceChanged(Entity<ToggleParticleVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<bool>(ent, ToggleableVisuals.Enabled, out var enabled, args.Component))
            return;

        SetActive(ent, ent.Comp, enabled);
    }

    private void SetActive(EntityUid uid, ToggleParticleVisualsComponent component, bool active)
    {
        if (!active)
        {
            Stop(uid);
            return;
        }

        var source = uid;
        var spawnOffset = GetGroundOffset(uid, component);
        if (_heldVisuals.TryGetValue(uid, out var heldVisual))
        {
            source = heldVisual.Holder;
            spawnOffset = heldVisual.SpawnOffset;
        }

        if (_active.TryGetValue(uid, out var activeOrchestra))
        {
            if (activeOrchestra.Context.Source == source)
            {
                _orchestra.UpdateSpawnOffset(activeOrchestra, spawnOffset);
                return;
            }

            Stop(uid);
        }

        if (_orchestra.Start(component.Orchestra, source, spawnOffset: spawnOffset) is { } orchestra)
            _active.Add(uid, orchestra);
    }

    private void Stop(EntityUid uid)
    {
        if (!_active.Remove(uid, out var orchestra))
            return;

        _orchestra.Stop(orchestra);
    }

    private bool TryGetHeldVisual(
        EntityUid holder,
        EntityUid item,
        HashSet<string> revealedLayers,
        out HeldParticleVisual heldVisual)
    {
        heldVisual = default;
        if (!TryComp<SpriteComponent>(holder, out var sprite))
            return false;

        TryComp<ToggleableVisualsComponent>(item, out var visuals);
        TryComp<ToggleableLightVisualsComponent>(item, out var lightVisuals);

        _heldLayerKeys.Clear();
        foreach (var key in revealedLayers)
        {
            if (IsToggleLayerKey(visuals, lightVisuals, key))
                _heldLayerKeys.Add(key);
        }

        if (_heldLayerKeys.Count == 0)
            return false;

        var layerKeys = _heldLayerKeys.ToArray();
        var spawnOffset = GetHeldSpawnOffset(holder, sprite, layerKeys);
        heldVisual = new HeldParticleVisual(
            holder,
            layerKeys,
            _anchors.GetFacingDirection(holder),
            sprite.Rotation,
            sprite.Offset,
            spawnOffset);
        return true;
    }

    private bool TryRefreshHeldVisual(
        EntityUid item,
        HeldParticleVisual heldVisual,
        out HeldParticleVisual refreshed)
    {
        refreshed = default;
        if (TerminatingOrDeleted(heldVisual.Holder) ||
            !_hands.IsHolding(heldVisual.Holder, item) ||
            !TryComp<SpriteComponent>(heldVisual.Holder, out var sprite))
        {
            return false;
        }

        refreshed = new HeldParticleVisual(
            heldVisual.Holder,
            heldVisual.LayerKeys,
            _anchors.GetFacingDirection(heldVisual.Holder),
            sprite.Rotation,
            sprite.Offset,
            GetHeldSpawnOffset(heldVisual.Holder, sprite, heldVisual.LayerKeys));
        return true;
    }

    private Vector2 GetHeldSpawnOffset(
        EntityUid holder,
        SpriteComponent sprite,
        IReadOnlyList<string> layerKeys)
    {
        _layerSamples.Clear();
        foreach (var key in layerKeys)
        {
            _anchors.AddOpaqueLayerSampleOffsets(
                (holder, sprite),
                key,
                _layerSamples,
                MaxLayerSamples - _layerSamples.Count);

            if (_layerSamples.Count >= MaxLayerSamples)
                break;
        }

        var spawnOffset = TryGetSampleCenter(out var sampleCenter)
            ? sampleCenter
            : _anchors.GetOffset(holder, ParticleVisualAnchor.Hands);
        return MoveToFlameTip(spawnOffset);
    }

    private Vector2 GetGroundOffset(EntityUid item, ToggleParticleVisualsComponent component)
    {
        if (component.SpriteLayer == null || !TryComp<SpriteComponent>(item, out var sprite))
            return MoveToFlameTip(component.FallbackOffset);

        _layerSamples.Clear();
        _anchors.AddOpaqueLayerSampleOffsets(
            (item, sprite),
            component.SpriteLayer,
            _layerSamples,
            MaxLayerSamples);

        var spawnOffset = TryGetSampleCenter(out var sampleCenter)
            ? sampleCenter
            : component.FallbackOffset;
        return MoveToFlameTip(spawnOffset);
    }

    private Vector2 MoveToFlameTip(Vector2 offset)
        => offset + _anchors.GetScreenUpWorldDirection() * FlameTipLift;

    private bool TryGetSampleCenter(out Vector2 center)
    {
        center = Vector2.Zero;
        if (_layerSamples.Count == 0)
            return false;

        foreach (var sample in _layerSamples)
            center += sample;

        center /= _layerSamples.Count;
        return true;
    }

    private static bool IsToggleLayerKey(
        ToggleableVisualsComponent? visuals,
        ToggleableLightVisualsComponent? lightVisuals,
        string key)
    {
        if (key.Contains(GeneratedToggleLayerMarker, StringComparison.Ordinal))
            return true;

        return (visuals != null && HasMappedInhandLayer(visuals.InhandVisuals, key)) ||
               (lightVisuals != null && HasMappedInhandLayer(lightVisuals.InhandVisuals, key));
    }

    private static bool HasMappedInhandLayer(
        Dictionary<HandLocation, List<PrototypeLayerData>> inhandVisuals,
        string key)
    {
        foreach (var layers in inhandVisuals.Values)
        {
            foreach (var layer in layers)
            {
                if (layer.MapKeys?.Contains(key) == true)
                    return true;
            }
        }

        return false;
    }

    private readonly record struct HeldParticleVisual(
        EntityUid Holder,
        string[] LayerKeys,
        Direction Facing,
        Angle SpriteRotation,
        Vector2 SpriteOffset,
        Vector2 SpawnOffset);
}
