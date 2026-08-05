// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared._Sunrise.GhostTheme;
using Content.Shared.Movement.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostThemeComponent, AfterAutoHandleStateEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, GhostThemeComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.GhostTheme == null
            || !_proto.TryIndex<GhostThemePrototype>(component.GhostTheme, out var ghostTheme))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), EffectLayers.Unshaded, out var layer, false))
        {
            _sprite.LayerSetSprite((uid, sprite), layer, ghostTheme.Sprite);
            sprite.LayerSetShader(layer, "unshaded");
            _sprite.LayerSetColor((uid, sprite), layer, ghostTheme.SpriteColor);
            _sprite.LayerSetScale((uid, sprite), layer, ghostTheme.Scale);
        }

        _sprite.SetDrawDepth((uid, sprite), DrawDepth.Default + 11);
        sprite.OverrideContainerOcclusion = true;
        sprite.NoRotation = true;
    }
}
