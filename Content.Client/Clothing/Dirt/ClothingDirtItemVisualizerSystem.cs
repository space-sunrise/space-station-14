using Content.Shared.Clothing.Dirt;
using Robust.Client.GameObjects;

namespace Content.Client.Clothing.Dirt;

/// <summary>
/// Отображает загрязнение на спрайте предмета одежды когда он лежит на полу или в инвентаре.
/// Дополняет ClothingDirtBodyVisualizerSystem (которая работает с телом персонажа).
/// </summary>
public sealed class ClothingDirtItemVisualizerSystem : EntitySystem
{
    private const string DirtLayerKey = "dirt_item_overlay";
    private const string DirtOverlayTexture = "/Textures/Clothing/dirt_overlay.rsi";
    private const string DirtOverlayState = "dirt";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingDirtComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingDirtComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ClothingDirtComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, ClothingDirtComponent component, ComponentStartup args)
        => UpdateItemSprite(uid, component);

    private void OnHandleState(EntityUid uid, ClothingDirtComponent component, ref ComponentHandleState args)
        => UpdateItemSprite(uid, component);

    private void OnShutdown(EntityUid uid, ClothingDirtComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(DirtLayerKey, out var idx))
            sprite.LayerSetVisible(idx, false);
    }

    private void UpdateItemSprite(EntityUid uid, ClothingDirtComponent component)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (component.DirtLevel <= 0f)
        {
            if (sprite.LayerMapTryGet(DirtLayerKey, out var hideIdx))
                sprite.LayerSetVisible(hideIdx, false);
            return;
        }

        if (!sprite.LayerMapTryGet(DirtLayerKey, out var layerIdx))
        {
            layerIdx = sprite.AddLayer(
                new SpriteSpecifier.Rsi(new ResPath(DirtOverlayTexture), DirtOverlayState));
            sprite.LayerMapSet(DirtLayerKey, layerIdx);
        }

        sprite.LayerSetVisible(layerIdx, true);
        var alpha = MathHelper.Lerp(0.15f, 0.85f, component.DirtLevel);
        sprite.LayerSetColor(layerIdx, component.DirtColor.WithAlpha(alpha));
    }
}
