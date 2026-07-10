using Content.Shared._Sunrise.Light.Visualizers;
using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Sunrise.Light.Visualizers;

public sealed class SunrisePoweredLightSparksSystem : EntitySystem
{
    public const string DefaultLayer = "sunrisePoweredLightSparks";

    [Dependency] private readonly AppearanceSystem _appearance = default!;
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

        var sprite = (ent.Owner, args.Sprite);
        if (!_appearance.TryGetData<PoweredLightState>(ent, PoweredLightVisuals.BulbState, out var state, args.Component) ||
            state != PoweredLightState.Broken ||
            !TryComp<PointLightComponent>(ent, out _))
        {
            SetLayerVisible(sprite, ent.Comp.Layer, false);
            SetLayerVisible(sprite, ent.Comp.SparksLayer, false);
            return;
        }

        SelectStates(ent);
        if (ent.Comp.SelectedState == null || ent.Comp.SelectedSparkState == null)
            return;

        UpdateLayer(sprite, ent.Comp.Layer, ent.Comp.SelectedState, ent.Comp.SparkSprite);
        UpdateLayer(sprite, ent.Comp.SparksLayer, ent.Comp.SelectedSparkState, ent.Comp.SparkSprite);
    }

    private void SelectStates(Entity<SunrisePoweredLightSparksComponent> ent)
    {
        if (ent.Comp.SelectedState != null && ent.Comp.SelectedSparkState != null)
            return;

        if (ent.Comp.States.Count == 0 || ent.Comp.SparkStates.Count == 0)
            return;

        var seed = unchecked((uint) GetNetEntity(ent).Id);
        ent.Comp.SelectedState = ent.Comp.States[(int) (seed % ent.Comp.States.Count)];
        ent.Comp.SelectedSparkState = ent.Comp.SparkStates[(int) ((seed * 2654435761u) % ent.Comp.SparkStates.Count)];
    }

    private void UpdateLayer(Entity<SpriteComponent> sprite, string layerKey, string state, ResPath? fallbackRsi)
    {
        var layer = EnsureSparkLayer(sprite, layerKey);
        if (layer == null)
            return;

        if (!TrySetState(layer, state, fallbackRsi))
        {
            _sprite.LayerSetVisible(layer, false);
            _sprite.LayerSetAutoAnimated(layer, false);
            return;
        }

        _sprite.LayerSetVisible(layer, true);
        _sprite.LayerSetAutoAnimated(layer, true);
    }

    private bool TrySetState(Layer layer, string state, ResPath? fallbackRsi)
    {
        if (layer.ActualRsi?.TryGetState(state, out _) == true)
        {
            _sprite.LayerSetRsiState(layer, state);
            return true;
        }

        if (fallbackRsi == null)
            return false;

        _sprite.LayerSetRsi(layer, fallbackRsi.Value);
        if (layer.ActualRsi?.TryGetState(state, out _) != true)
            return false;

        _sprite.LayerSetRsiState(layer, state);
        return true;
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

    private void SetLayerVisible(Entity<SpriteComponent> sprite, string layerKey, bool visible)
    {
        if (!_sprite.LayerMapTryGet(sprite.AsNullable(), layerKey, out var layerIndex, false))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), layerIndex, visible);
        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), layerIndex, visible);
    }
}
