using System.Linq;
using System.Numerics;
using Content.Client._Sunrise.MarkingEffectsClient;
using Content.Shared._Sunrise;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed partial class HumanoidAppearanceSystem
{
    private static readonly float MirrorPixelCompensation = 1f / EyeManager.PixelsPerMeter;

    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private readonly Dictionary<EntityUid, HairMirroringCache> _hairMirroringCache = new();

    private void InitializeSunrise()
    {
        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentShutdown>(OnHumanoidShutdown);
        SubscribeLocalEvent<HumanoidAppearanceComponent, MoveEvent>(OnMoved);
    }

    private void UpdateSpriteSunrise(Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        var scale = new Vector2(entity.Comp1.Width, entity.Comp1.Height);
        _sprite.SetScale(entity.Owner, scale);
    }

    private BodyTypePrototype GetBodyTypePrototypeSunrise(HumanoidAppearanceComponent component)
    {
        return _prototypeManager.Index(component.BodyType);
    }

    private void LoadProfileSunrise(HumanoidCharacterProfile profile, HumanoidAppearanceComponent humanoid)
    {
        humanoid.Width = profile.Appearance.Width;
        humanoid.Height = profile.Appearance.Height;
        humanoid.HairMirrored = profile.Appearance.HairMirrored;
    }

    private Marking CreateHairMarkingSunrise(HumanoidCharacterProfile profile, Color hairColor)
    {
        var hairMarkingEffects = profile.Appearance.HairMarkingEffect != null
            ? new List<MarkingEffect> { profile.Appearance.HairMarkingEffect }
            : new List<MarkingEffect>();

        return new Marking(profile.Appearance.HairStyleId,
            new[] { hairColor },
            hairMarkingEffects);
    }

    private Marking CreateFacialHairMarkingSunrise(HumanoidCharacterProfile profile, Color facialHairColor)
    {
        var facialHairMarkingEffects = profile.Appearance.FacialHairMarkingEffect != null
            ? new List<MarkingEffect> { profile.Appearance.FacialHairMarkingEffect }
            : new List<MarkingEffect>();

        return new Marking(profile.Appearance.FacialHairStyleId,
            new[] { facialHairColor },
            facialHairMarkingEffects);
    }

    public void UpdateHairMirroringForDirection(EntityUid uid, Direction direction)
    {
        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        UpdateHairMirroring((uid, humanoid, sprite), direction);
    }

    private SunriseMarkingSetState? CreateMarkingSetStateSunrise(Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        if (!entity.Comp1.HairMirrored)
            return null;

        return new SunriseMarkingSetState
        {
            VisualDirection = GetCurrentVisualDirection(entity.Owner),
        };
    }

    private void ApplyMarkingSunrise(MarkingPrototype markingPrototype, IReadOnlyList<MarkingEffect>? markingEffects,
        bool visible, Entity<HumanoidAppearanceComponent, SpriteComponent> entity, SunriseMarkingSetState? state)
    {
        var humanoid = entity.Comp1;
        var sprite = entity.Comp2;

        var shouldMirror = markingPrototype.BodyPart == HumanoidVisualLayers.Hair && humanoid.HairMirrored;


        var visualDirection = Direction.Invalid;

        if (shouldMirror)
            visualDirection = state?.VisualDirection ?? GetCurrentVisualDirection(entity.Owner);

        var shouldApplyEffects = visible &&
            humanoid.BaseLayers.TryGetValue(markingPrototype.BodyPart, out var setting) &&
            setting.AllowsMarkings;

        var targetLayer = 0;

        if (shouldApplyEffects && !_sprite.LayerMapTryGet((entity.Owner, sprite), markingPrototype.BodyPart, out targetLayer, false))
            shouldApplyEffects = false;

        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            var markingSprite = markingPrototype.Sprites[j];
            if (markingSprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (shouldMirror)
            {
                state?.LayerIds.Add(layerId);
                ApplyHairMirroring((entity.Owner, sprite), layerId, true, visualDirection);
            }

            if (!shouldApplyEffects)
                continue;

            ShaderInstance? shaderOverride = null;
            if (markingEffects != null && j < markingEffects.Count && markingEffects[j].Type != MarkingEffectType.Color)
            {
                float texWidth = sprite.AllLayers.Max(x => x.PixelSize.X);
                float texHeight = sprite.AllLayers.Max(x => x.PixelSize.Y);
                var shaderName = markingEffects[j].Type.ToString();
                var instance = _prototypeManager.Index<ShaderPrototype>(shaderName).InstanceUnique();
                shaderOverride = instance;

                instance.ApplyShaderParams(markingEffects[j], new Vector2(texWidth, texHeight));

                sprite.LayerSetShader(layerId, instance);
                _sprite.LayerSetColor((entity.Owner, sprite), layerId, Color.White);
            }

            var displacementData = GetMarkingDisplacement(entity.Owner, markingPrototype.BodyPart, humanoid);

            if (displacementData != null && markingPrototype.CanBeDisplaced)
            {
                _displacement.TryAddDisplacement(
                    displacementData,
                    (entity.Owner, sprite),
                    targetLayer + j + 1,
                    layerId,
                    out _,
                    shaderOverride);
            }
        }
    }

    private void FinalizeMarkingSetSunrise(EntityUid uid, SunriseMarkingSetState? state)
    {
        if (state == null || state.LayerIds.Count == 0)
        {
            _hairMirroringCache.Remove(uid);
            return;
        }

        if (!_hairMirroringCache.TryGetValue(uid, out var cache))
        {
            cache = new HairMirroringCache();
            _hairMirroringCache[uid] = cache;
        }

        cache.LayerIds.Clear();
        cache.LayerIds.AddRange(state.LayerIds);
        cache.LastDirection = state.VisualDirection;
    }

    private void OnHumanoidShutdown(EntityUid uid, HumanoidAppearanceComponent component, ref ComponentShutdown args)
    {
        _hairMirroringCache.Remove(uid);
    }

    private void OnMoved(EntityUid uid, HumanoidAppearanceComponent component, ref MoveEvent args)
    {
        if (!component.HairMirrored ||
            args.OldRotation.GetCardinalDir() == args.NewRotation.GetCardinalDir() ||
            !TryComp(uid, out SpriteComponent? sprite))
        {
            return;
        }

        UpdateHairMirroring((uid, component, sprite));
    }

    private Direction GetCurrentVisualDirection(EntityUid uid)
    {
        var angle = _transform.GetWorldRotation(uid) + _eyeManager.CurrentEye.Rotation;
        return angle.GetCardinalDir();
    }

    private static SpriteComponent.DirectionOffset GetHairDirOffset(Direction direction, bool shouldMirror)
    {
        if (!shouldMirror)
            return SpriteComponent.DirectionOffset.None;

        return direction is Direction.East or Direction.West
            ? SpriteComponent.DirectionOffset.Flip
            : SpriteComponent.DirectionOffset.None;
    }

    private static bool ShouldApplyMirrorCompensation(Direction direction, bool shouldMirror)
    {
        return shouldMirror && direction is Direction.North or Direction.South;
    }

    private void ApplyHairMirroring(Entity<SpriteComponent> spriteEnt, string layerId, bool shouldMirror, Direction direction)
    {
        if (!_sprite.TryGetLayer((spriteEnt.Owner, spriteEnt.Comp), layerId, out var existingLayer, false))
            return;

        var offset = existingLayer.Offset;
        var targetDirOffset = GetHairDirOffset(direction, shouldMirror);
        var hadMirrorCompensation = existingLayer.Scale.X < 0f && existingLayer.DirOffset == SpriteComponent.DirectionOffset.None;
        var shouldApplyCompensation = ShouldApplyMirrorCompensation(direction, shouldMirror);

        if (hadMirrorCompensation)
            offset.X += MirrorPixelCompensation;

        if (shouldApplyCompensation)
            offset.X -= MirrorPixelCompensation;

        _sprite.LayerSetDirOffset((spriteEnt.Owner, spriteEnt.Comp), layerId, targetDirOffset);
        _sprite.LayerSetOffset((spriteEnt.Owner, spriteEnt.Comp), layerId, offset);
        _sprite.LayerSetScale((spriteEnt.Owner, spriteEnt.Comp), layerId, shouldMirror ? new Vector2(-1f, 1f) : Vector2.One);
    }

    private void UpdateHairMirroring(Entity<HumanoidAppearanceComponent, SpriteComponent> entity, Direction? forcedDirection = null)
    {
        if (!entity.Comp1.HairMirrored)
            return;

        var visualDirection = forcedDirection ?? GetCurrentVisualDirection(entity.Owner);
        if (!_hairMirroringCache.TryGetValue(entity.Owner, out var cache) ||
            cache.LayerIds.Count == 0 ||
            cache.LastDirection == visualDirection)
        {
            return;
        }

        foreach (var layerId in cache.LayerIds)
        {
            ApplyHairMirroring((entity.Owner, entity.Comp2), layerId, true, visualDirection);
        }

        cache.LastDirection = visualDirection;
    }

    private sealed class SunriseMarkingSetState
    {
        public readonly List<string> LayerIds = new();
        public Direction VisualDirection = Direction.Invalid;
    }

    private sealed class HairMirroringCache
    {
        public readonly List<string> LayerIds = new();
        public Direction LastDirection = Direction.Invalid;
    }
}
