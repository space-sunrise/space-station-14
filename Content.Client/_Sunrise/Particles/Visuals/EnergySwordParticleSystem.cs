using System.Numerics;
using Content.Client.Toggleable;
using Content.Shared._Sunrise.Particles;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.EnergySword;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Adds blade-tinted ignition effects and a fixed thermal melt mark when an active energy sword strikes a structure.
/// </summary>
public sealed class EnergySwordParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly ParticleVisualAnchorSystem _anchors = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const int HolderSearchDepth = 4;
    private const int MaxIgnitionSamples = 6;
    private const string GeneratedToggleLayerMarker = "-toggle";

    private static readonly ProtoId<ParticleOrchestraPrototype> IgnitionOrchestra = "EnergySwordIgnition";
    private static readonly ProtoId<ParticleOrchestraPrototype> HeatMarkOrchestra = "EnergySwordHeatMark";
    private static readonly ProtoId<TagPrototype>[] HeatMarkTargetTags =
        ["Wall", "Airlock", "GlassAirlock", "HighSecDoor", "Window"];

    private readonly Dictionary<EntityUid, HeldBladeVisual> _heldBladeVisuals = new();
    private readonly Dictionary<EntityUid, Color> _pendingIgnitions = new();
    private readonly List<Vector2> _ignitionOffsets = new(MaxIgnitionSamples);
    private EntityQuery<HandsComponent> _handsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _handsQuery = GetEntityQuery<HandsComponent>();

        SubscribeLocalEvent<EnergySwordComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<EnergySwordComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<EnergySwordComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<EnergySwordComponent, AppearanceChangeEvent>(OnAppearanceChanged,
            after: [typeof(ToggleableVisualsSystem), typeof(ToggleableLightVisualsSystem)]);
        SubscribeLocalEvent<EnergySwordComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _heldBladeVisuals.Clear();
        _pendingIgnitions.Clear();
        _ignitionOffsets.Clear();
    }

    private void OnToggled(Entity<EnergySwordComponent> ent, ref ItemToggledEvent args)
    {
        // Непредсказываемые переключения приходят один раз; replay нужен только для predicted-варианта события.
        if (args.Predicted && !_timing.IsFirstTimePredicted)
            return;

        if (!args.Activated)
        {
            _pendingIgnitions.Remove(ent);
            return;
        }

        _pendingIgnitions[ent] = ent.Comp.ActivatedColor;
    }

    private void OnMeleeHit(Entity<EnergySwordComponent> ent, ref MeleeHitEvent args)
    {
        // Попадание — одноразовый визуальный эффект, который нельзя повторять на каждом replay.
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!TryComp<ItemToggleComponent>(ent, out var toggle) || !toggle.Activated)
            return;

        foreach (var target in args.HitEntities)
            TrySpawnImpact(target, args.User);
    }

    private void OnHeldVisualsUpdated(Entity<EnergySwordComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        TryComp<ToggleableVisualsComponent>(ent, out var visuals);
        TryComp<ToggleableLightVisualsComponent>(ent, out var lightVisuals);
        if (args.RevealedLayers.Count == 0 || (visuals == null && lightVisuals == null))
        {
            _heldBladeVisuals.Remove(ent);
            return;
        }

        var layerCount = 0;
        foreach (var key in args.RevealedLayers)
        {
            if (IsToggleLayerKey(visuals, lightVisuals, key))
                layerCount++;
        }

        if (layerCount == 0)
        {
            _heldBladeVisuals.Remove(ent);
            return;
        }

        var layerKeys = new string[layerCount];
        var index = 0;
        foreach (var key in args.RevealedLayers)
        {
            if (IsToggleLayerKey(visuals, lightVisuals, key))
                layerKeys[index++] = key;
        }

        _heldBladeVisuals[ent] = new HeldBladeVisual(args.User, layerKeys);
    }

    private void OnAppearanceChanged(Entity<EnergySwordComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_pendingIgnitions.Remove(ent, out var color) ||
            !TryComp<ItemToggleComponent>(ent, out var toggle) ||
            !toggle.Activated)
        {
            return;
        }

        SpawnIgnition(ent, color);
    }

    private void OnComponentShutdown(Entity<EnergySwordComponent> ent, ref ComponentShutdown args)
    {
        _heldBladeVisuals.Remove(ent);
        _pendingIgnitions.Remove(ent);
    }

    private void SpawnIgnition(EntityUid weapon, Color color)
    {
        _ignitionOffsets.Clear();

        var source = GetVisualSource(weapon);
        if (source != weapon &&
            _heldBladeVisuals.TryGetValue(weapon, out var heldVisual) &&
            heldVisual.Holder == source &&
            TryComp<SpriteComponent>(source, out var holderSprite))
        {
            for (var i = 0; i < heldVisual.LayerKeys.Length; i++)
            {
                var remainingSamples = MaxIgnitionSamples - _ignitionOffsets.Count;
                if (remainingSamples <= 0)
                    break;

                var remainingLayers = heldVisual.LayerKeys.Length - i;
                var layerSampleLimit = Math.Max(1, remainingSamples / remainingLayers);

                _anchors.AddOpaqueLayerSampleOffsets(
                    (source, holderSprite),
                    heldVisual.LayerKeys[i],
                    _ignitionOffsets,
                    layerSampleLimit);
            }
        }
        else if (source == weapon &&
                 TryComp<SpriteComponent>(weapon, out var weaponSprite) &&
                 TryGetToggleSpriteLayer(weapon, out var spriteLayer))
        {
            _anchors.AddOpaqueLayerSampleOffsets(
                (weapon, weaponSprite),
                spriteLayer,
                _ignitionOffsets,
                MaxIgnitionSamples);
        }

        if (_ignitionOffsets.Count == 0)
        {
            SpawnIgnitionFallback(weapon, source, color);
            return;
        }

        var tint = ParticleColorHelper.EnsureVisible(color);
        var sourceCoordinates = _transform.GetMapCoordinates(source);

        foreach (var offset in _ignitionOffsets)
        {
            var direction = offset.LengthSquared() > 0.0001f
                ? offset
                : _anchors.GetFacingWorldDirection(source);
            _orchestra.StartAt(
                IgnitionOrchestra,
                sourceCoordinates,
                source,
                movement: direction,
                colorOverride: tint,
                spawnOffset: offset);
        }
    }

    private void SpawnIgnitionFallback(EntityUid weapon, EntityUid source, Color color)
    {
        var tint = ParticleColorHelper.EnsureVisible(color);
        var weaponCoordinates = _transform.GetMapCoordinates(weapon);
        var sourceCoordinates = _transform.GetMapCoordinates(source);
        if (weaponCoordinates.MapId != sourceCoordinates.MapId)
            return;

        var spawnOffset = sourceCoordinates.Position - weaponCoordinates.Position;
        if (source != weapon)
            spawnOffset += _anchors.GetOffset(source, ParticleVisualAnchor.Hands);

        _orchestra.StartAt(
            IgnitionOrchestra,
            weaponCoordinates,
            source,
            movement: _anchors.GetFacingWorldDirection(source),
            colorOverride: tint,
            spawnOffset: spawnOffset);
    }

    private bool TrySpawnImpact(EntityUid target, EntityUid user)
    {
        if (!CanSpawnImpact(target, user))
            return false;

        var targetCoordinates = _transform.GetMapCoordinates(target);
        var userCoordinates = _transform.GetMapCoordinates(user);
        if (targetCoordinates.MapId != userCoordinates.MapId)
            return false;

        var attackOrigin = userCoordinates.Position + _anchors.GetOffset(user, ParticleVisualAnchor.Hands);
        var contactOffset = _anchors.GetVisualEdgeOffset(target, attackOrigin, 0.42f);

        _orchestra.StartAt(
            HeatMarkOrchestra,
            targetCoordinates,
            source: target,
            spawnOffset: contactOffset);

        return true;
    }

    private bool CanSpawnImpact(EntityUid target, EntityUid user)
    {
        return !TerminatingOrDeleted(target) &&
               !TerminatingOrDeleted(user) &&
               _tag.HasAnyTag(target, HeatMarkTargetTags);
    }

    private EntityUid GetVisualSource(EntityUid weapon)
    {
        var current = weapon;
        for (var i = 0; i < HolderSearchDepth; i++)
        {
            var parent = _transform.GetParentUid(current);
            if (!parent.IsValid() || parent == current)
                break;

            if (_handsQuery.HasComp(parent))
                return parent;

            current = parent;
        }

        return weapon;
    }

    private bool TryGetToggleSpriteLayer(EntityUid weapon, out string spriteLayer)
    {
        if (TryComp<ToggleableVisualsComponent>(weapon, out var visuals) &&
            visuals.SpriteLayer is { } layer)
        {
            spriteLayer = layer;
            return true;
        }

        if (TryComp<ToggleableLightVisualsComponent>(weapon, out var lightVisuals) &&
            lightVisuals.SpriteLayer is { } lightLayer)
        {
            spriteLayer = lightLayer;
            return true;
        }

        spriteLayer = string.Empty;
        return false;
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

    private readonly record struct HeldBladeVisual(EntityUid Holder, string[] LayerKeys);
}
