using Content.Shared._Sunrise.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Weapons.Ranged;

public sealed class BulletHoleVisualizerSystem : VisualizerSystem<BulletHoleComponent>
{
    private static readonly ResPath RsiPath = new("/Textures/_Sunrise/Effects/bulletholes.rsi");

    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, BulletHoleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, BulletHoleVisuals.State, out var state, args.Component))
            return;

        var layer = _sprite.LayerMapReserve((uid, sprite), BulletHoleVisualLayers.BulletHole);
        var visible = !string.IsNullOrWhiteSpace(state);
        _sprite.LayerSetVisible((uid, sprite), layer, visible);

        if (!visible)
            return;

        _sprite.LayerSetRsi((uid, sprite), layer, RsiPath);
        _sprite.LayerSetRsiState((uid, sprite), layer, state);
    }
}
