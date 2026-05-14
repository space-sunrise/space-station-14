// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared._Sunrise.GhostTheme;
using Content.Shared.DrawDepth;
using ContentDrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Content.Shared.Weapons.Ranged.Systems; // EffectLayers
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostThemeComponent, AfterAutoHandleStateEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, GhostThemeComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.GhostTheme == null
            || !_prototypeManager.TryIndex<GhostThemePrototype>(component.GhostTheme, out var ghostThemePrototype))
            return;

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!sprite.LayerMapTryGet(EffectLayers.Unshaded, out var layer))
            layer = sprite.LayerMapReserveBlank(EffectLayers.Unshaded);

        sprite.LayerSetSprite(layer, ghostThemePrototype.Sprite);
        sprite.LayerSetShader(layer, "unshaded");
        sprite.LayerSetColor(layer, ghostThemePrototype.SpriteColor);
        sprite.LayerSetScale(layer, ghostThemePrototype.Scale);

        sprite.DrawDepth = (int)ContentDrawDepth.Ghosts;
        sprite.OverrideContainerOcclusion = true;
        sprite.NoRotation = true;
    }
}
