using Content.Shared._Sunrise.Footprints;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Client._Sunrise.Footprints;

/// <summary>
/// Handles the visual appearance and updates of footprint entities on the client
/// </summary>
public sealed class FootprintVisualizerSystem : VisualizerSystem<FootprintComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _showFootprints;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.ShowFootprints, OnShowFootprintsChanged, true);

        SubscribeLocalEvent<FootprintComponent, ComponentInit>(OnFootprintInitialized);
        SubscribeLocalEvent<FootprintComponent, ComponentShutdown>(OnFootprintShutdown);
    }

    private void OnShowFootprintsChanged(bool value)
    {
        _showFootprints = value;
        var query = EntityManager.AllEntityQueryEnumerator<FootprintComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var footprint, out var sprite))
        {
            UpdateFootprintVisuals(uid, footprint, sprite);
        }
    }

    /// <summary>
    /// Initializes the visual appearance of a new footprint
    /// </summary>
    private void OnFootprintInitialized(EntityUid uid, FootprintComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        InitializeSpriteLayers(sprite);
        UpdateFootprintVisuals(uid, component, sprite);
    }

    /// <summary>
    /// Cleans up the visual elements when a footprint is removed
    /// </summary>
    private void OnFootprintShutdown(EntityUid uid, FootprintComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        RemoveSpriteLayers(sprite);
    }

    /// <summary>
    /// Sets up the initial sprite layers for the footprint
    /// </summary>
    private void InitializeSpriteLayers(SpriteComponent sprite)
    {
        sprite.LayerMapReserveBlank(FootprintSpriteLayer.MainLayer);
    }

    /// <summary>
    /// Removes sprite layers when cleaning up footprint
    /// </summary>
    private void RemoveSpriteLayers(SpriteComponent sprite)
    {
        if (sprite.LayerMapTryGet(FootprintSpriteLayer.MainLayer, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    /// <summary>
    /// Updates the visual appearance of a footprint based on its current state
    /// </summary>
    private void UpdateFootprintVisuals(EntityUid uid, FootprintComponent footprint, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(FootprintSpriteLayer.MainLayer, out var layer)
            || !TryComp<FootprintEmitterComponent>(footprint.CreatorEntity, out var emitterComponent)
            || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        // Hide footprints if disabled in settings
        if (!_showFootprints)
        {
            sprite.Visible = false;
            return;
        }

        sprite.Visible = true;

        if (!_appearanceSystem.TryGetData<FootprintVisualType>(
                uid,
                FootprintVisualParameter.VisualState,
                out var visualType,
                appearance))
            return;

        UpdateSpriteState(sprite, layer, visualType, emitterComponent);
        UpdateSpriteColor(sprite, layer, uid, appearance);
    }

    /// <summary>
    /// Updates the sprite state based on the footprint type
    /// </summary>
    private void UpdateSpriteState(
        SpriteComponent sprite,
        int layer,
        FootprintVisualType visualType,
        FootprintEmitterComponent emitter)
    {
        var stateId = new RSI.StateId(GetStateId(visualType, emitter));
        sprite.LayerSetState(layer, stateId, emitter.SpritePath);
    }

    /// <summary>
    /// Determines the appropriate state ID for the footprint based on its type
    /// </summary>
    private string GetStateId(FootprintVisualType visualType, FootprintEmitterComponent emitter)
    {
        return visualType switch
        {
            FootprintVisualType.BareFootprint => emitter.IsRightStep
                ? emitter.RightBareFootState
                : emitter.LeftBareFootState,
            FootprintVisualType.ShoeFootprint => emitter.ShoeFootState,
            FootprintVisualType.SuitFootprint => emitter.PressureSuitFootState,
            FootprintVisualType.DragMark => _random.Pick(emitter.DraggingStates),
            _ => throw new ArgumentOutOfRangeException(
                $"Unknown footprint visual type: {visualType}")
        };
    }

    /// <summary>
    /// Updates the sprite color based on appearance data
    /// </summary>
    private void UpdateSpriteColor(
        SpriteComponent sprite,
        int layer,
        EntityUid uid,
        AppearanceComponent appearance)
    {
        if (_appearanceSystem.TryGetData<Color>(uid,
                FootprintVisualParameter.TrackColor,
                out var color,
                appearance))
        {
            sprite.LayerSetColor(layer, color);
        }
    }

    /// <inheritdoc/>
    protected override void OnAppearanceChange(
        EntityUid uid,
        FootprintComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        UpdateFootprintVisuals(uid, component, sprite);
    }
}
