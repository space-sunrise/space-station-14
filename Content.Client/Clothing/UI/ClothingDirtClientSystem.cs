using Content.Client.Clothing.Dirt.UI;
using Content.Shared.Clothing.Dirt;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Clothing.Dirt;

public sealed class ClothingDirtClientSystem : SharedClothingDirtSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    private ClothingDirtOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ClothingDirtOverlay();
        _overlays.AddOverlay(_overlay);

        SubscribeLocalEvent<ClothingDirtComponent, ComponentHandleState>(OnState);
        SubscribeLocalEvent<ClothingDirtComponent, ComponentRemove>(OnRemove);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlays.RemoveOverlay(_overlay);
    }

    private void OnState(EntityUid uid, ClothingDirtComponent dirt, ref ComponentHandleState _)
        => UpdateSprite(uid, dirt);

    private void OnRemove(EntityUid uid, ClothingDirtComponent _, ComponentRemove __)
        => UpdateSprite(uid, null);

    private void UpdateSprite(EntityUid uid, ClothingDirtComponent? dirt)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        const string key = "dirt";

        if (sprite.LayerMapTryGet(key, out var idx))
            sprite.RemoveLayer(idx);

        if (dirt == null || dirt.DirtLevel <= 0f)
            return;

        var newIdx = sprite.AddLayer(new SpriteComponent.Layer
        {
            TexturePath = "/Textures/Interface/dirt_overlay.png",
            Color = dirt.DirtColor.WithAlpha(dirt.DirtLevel / 100f * 0.7f),
            Visible = true,
        });
        sprite.LayerMapSet(key, newIdx);
    }
}
