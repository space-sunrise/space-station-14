using Content.Shared._Sunrise.Footprints;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Footprints;

/// <summary>
/// Handles the visual appearance and updates of footprint entities on the client
/// </summary>
public sealed class FootprintVisualizerSystem : VisualizerSystem<FootprintComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootprintComponent, ComponentInit>(OnFootprintInitialized);
        SubscribeLocalEvent<FootprintComponent, ComponentShutdown>(OnFootprintShutdown);
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
            || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        UpdateSpriteState(sprite, layer,footprint.StateId, footprint.SpritePath);
        UpdateSpriteColor(sprite, layer, uid, appearance);
    }

    /// <summary>
    /// Updates the sprite state based on the footprint type
    /// </summary>
    private void UpdateSpriteState(
        SpriteComponent sprite,
        int layer,
        string state,
        ResPath spritePath)
    {
        var stateId = new RSI.StateId(state);
        sprite.LayerSetState(layer, stateId, spritePath);
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
