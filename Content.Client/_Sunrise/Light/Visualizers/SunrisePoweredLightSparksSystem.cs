using Content.Shared._Sunrise.Light.Visualizers;
using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Sunrise.Light.Visualizers;

public sealed class SunrisePoweredLightSparksSystem : EntitySystem
{
    public const string DefaultLayer = "sunrisePoweredLightSparks";

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SunrisePoweredLightSparksComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<SunrisePoweredLightSparksComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<PoweredLightState>(ent, PoweredLightVisuals.BulbState, out var state, args.Component))
            return;

        if (ent.Comp.States.Count == 0)
            return;

        var layer = EnsureSparkLayer((ent.Owner, args.Sprite), ent.Comp.Layer);
        if (layer == null)
            return;

        if (!TryGetSparkState(ent.Comp, layer.ActualRsi, out var sparkState) &&
            (ent.Comp.SparkSprite == null || !TryUseSparkSprite(ent.Comp, layer, out sparkState)))
        {
            _sprite.LayerSetVisible(layer, false);
            _sprite.LayerSetAutoAnimated(layer, false);
            return;
        }

        var showSparks = state == PoweredLightState.Broken;
        _sprite.LayerSetRsiState(layer, sparkState);
        _sprite.LayerSetVisible(layer, showSparks);
        _sprite.LayerSetAutoAnimated(layer, showSparks);
    }

    private bool TryGetSparkState(SunrisePoweredLightSparksComponent component, RSI? rsi, out string state)
    {
        if (rsi == null)
        {
            state = string.Empty;
            return false;
        }

        if (component.SelectedState != null && rsi.TryGetState(component.SelectedState, out _))
        {
            state = component.SelectedState;
            return true;
        }

        var availableStates = new List<string>();
        foreach (var possibleState in component.States)
        {
            if (rsi.TryGetState(possibleState, out _))
                availableStates.Add(possibleState);
        }

        if (availableStates.Count == 0)
        {
            component.SelectedState = null;
            state = string.Empty;
            return false;
        }

        state = _random.Pick(availableStates);
        component.SelectedState = state;
        return true;
    }

    private bool TryUseSparkSprite(SunrisePoweredLightSparksComponent component, Layer layer, out string state)
    {
        if (component.SparkSprite == null)
        {
            state = string.Empty;
            return false;
        }

        _sprite.LayerSetRsi(layer, component.SparkSprite.Value);
        return TryGetSparkState(component, layer.ActualRsi, out state);
    }

    private Layer? EnsureSparkLayer(Entity<SpriteComponent> sprite, string layerKey)
    {
        var layerIndex = _sprite.LayerMapReserve(sprite.AsNullable(), layerKey);
        if (!_sprite.TryGetLayer(sprite.AsNullable(), layerIndex, out var layer, false))
            return null;

        _sprite.LayerSetData(layer, new PrototypeLayerData
        {
            Shader = SpriteSystem.UnshadedId.Id,
            Visible = false,
        });

        return layer;
    }
}
