using Content.Client._Fish.Mood;
using Content.Client._Sunrise.BloodCult;
using Content.Shared._Fish.Abilities.Milira;
using Content.Shared._Sunrise.Mood;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Fish.Mood;

/// <summary>
/// This handles the display of mood effects on entities with mood component.
/// </summary>
public sealed class MoodVisualizerSystem : VisualizerSystem<MoodVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MoodVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, MoodVisualsComponent component, ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            SpriteSystem.LayerMapTryGet((uid, sprite), MoodVisualLayers.Mood, out var layer, false))
        {
            SpriteSystem.RemoveLayer((uid, sprite), layer);
        }
    }

    private void OnComponentInit(EntityUid uid, MoodVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), MoodVisualLayers.Mood);
        SpriteSystem.LayerSetVisible((uid, sprite), MoodVisualLayers.Mood, false);
        sprite.LayerSetShader(MoodVisualLayers.Mood, "unshaded");
        if (component.Sprite != null)
            SpriteSystem.LayerSetRsi((uid, sprite), MoodVisualLayers.Mood, new ResPath(component.Sprite));

        UpdateAppearance(uid, component, sprite, appearance);
    }

    protected override void OnAppearanceChange(EntityUid uid, MoodVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, args.Component);
    }

    private bool ShouldHideMoodVisuals(EntityUid uid)
    {
        return HasComp<PentagramComponent>(uid) && HasComp<WingToggleComponent>(uid);
    }

    private void UpdateAppearance(EntityUid uid, MoodVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!SpriteSystem.LayerMapTryGet((uid, sprite), MoodVisualLayers.Mood, out var index, false))
            return;

        if (ShouldHideMoodVisuals(uid))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), index, false);
            return;
        }

        if (!AppearanceSystem.TryGetData<MoodThreshold>(uid, MoodVisuals.CurrentMoodThreshold, out var moodThreshold, appearance))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), index, false);
            return;
        }

        // Check if we have a sprite state for this mood threshold
        if (!component.MoodStates.TryGetValue(moodThreshold, out var state))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), index, false);
            return;
        }

        // Show the sprite layer and set the state
        SpriteSystem.LayerSetVisible((uid, sprite), index, true);
        SpriteSystem.LayerSetRsiState((uid, sprite), index, state);
    }
}

public enum MoodVisualLayers : byte
{
    Mood
}
