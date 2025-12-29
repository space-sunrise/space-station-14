using Content.Client._Sunrise.BloodCult;
using Content.Shared._Fish.Abilities.Milira;
using Content.Shared._Fish.Mood;
using Content.Shared._Sunrise.Mood;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Fish.Mood;

/// <summary>
/// This handles the display of mood effects on entities with mood component.
/// </summary>
public sealed class MoodVisualizerSystem : VisualizerSystem<MoodVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MoodVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<MoodVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (_spriteSystem.LayerMapTryGet((ent.Owner, sprite), MoodVisualLayers.Mood, out var layer, false))
            _spriteSystem.RemoveLayer((ent.Owner, sprite), layer);
    }

    private void OnComponentInit(Entity<MoodVisualsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        _spriteSystem.LayerMapReserve((ent.Owner, sprite), MoodVisualLayers.Mood);
        _spriteSystem.LayerSetVisible((ent.Owner, sprite), MoodVisualLayers.Mood, false);
        sprite.LayerSetShader(MoodVisualLayers.Mood, "unshaded");
        if (ent.Comp.Sprite != null)
            _spriteSystem.LayerSetSprite((ent.Owner, sprite), MoodVisualLayers.Mood, ent.Comp.Sprite);

        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            UpdateAppearance(ent.Owner, ent.Comp, sprite, appearance);
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
        if (!_spriteSystem.LayerMapTryGet((uid, sprite), MoodVisualLayers.Mood, out var index, false))
            return;

        if (ShouldHideMoodVisuals(uid))
        {
            _spriteSystem.LayerSetVisible((uid, sprite), index, false);
            return;
        }

        if (!_appearanceSystem.TryGetData<MoodThreshold>(uid, MoodVisuals.CurrentMoodThreshold, out var moodThreshold, appearance))
        {
            _spriteSystem.LayerSetVisible((uid, sprite), index, false);
            return;
        }

        // Check if we have a sprite state for this mood threshold
        if (!component.MoodStates.TryGetValue(moodThreshold, out var state))
        {
            _spriteSystem.LayerSetVisible((uid, sprite), index, false);
            return;
        }

        // Show the sprite layer and set the state
        _spriteSystem.LayerSetVisible((uid, sprite), index, true);
        _spriteSystem.LayerSetRsiState((uid, sprite), index, state);
    }
}

public enum MoodVisualLayers : byte
{
    Mood
}
