using System.Numerics;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Antags.Vampires.Overlays;

/// <summary>
/// Overlay that renders monster/animal sprites over humanoids
/// when the local player has HysteriaVisionComponent.
/// </summary>
public sealed class HysteriaVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float ViewBoundsMargin = 2f;

    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly EntityQuery<HysteriaVisionComponent> _hysteriaQuery;
    private readonly EntityQuery<VampireThrallComponent> _thrallQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly Dictionary<EntityUid, int> _entityDisguiseIndex = [];
    private readonly List<LoadedDisguise> _loadedDisguises = [];
    private readonly List<HysteriaDisguiseSprite> _cachedDisguiseSprites = [];
    private bool _disguisesLoaded;

    public HysteriaVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entManager.System<TransformSystem>();
        _sprite = _entManager.System<SpriteSystem>();
        _hysteriaQuery = _entManager.GetEntityQuery<HysteriaVisionComponent>();
        _thrallQuery = _entManager.GetEntityQuery<VampireThrallComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalEntity;
        if (player is null)
            return false;

        if (!_hysteriaQuery.TryGetComponent(player.Value, out var hysteria))
            return false;

        if (_timing.CurTime > hysteria.EndTime) // Check if effect expired
            return false;

        if (hysteria.DisguiseSprites.Count == 0)
            return false;

        EnsureDisguisesLoaded(hysteria);

        return _disguisesLoaded;
    }

    private void EnsureDisguisesLoaded(HysteriaVisionComponent hysteria)
    {
        if (_disguisesLoaded && IsDisguiseCacheCurrent(hysteria.DisguiseSprites))
            return;

        LoadDisguises(hysteria.DisguiseSprites);
    }

    private bool IsDisguiseCacheCurrent(IReadOnlyList<HysteriaDisguiseSprite> disguises)
    {
        if (_cachedDisguiseSprites.Count != disguises.Count)
            return false;

        for (var i = 0; i < disguises.Count; i++)
        {
            if (!_cachedDisguiseSprites[i].Equals(disguises[i]))
                return false;
        }

        return true;
    }

    private void LoadDisguises(IReadOnlyList<HysteriaDisguiseSprite> disguises)
    {
        _disguisesLoaded = false;
        _loadedDisguises.Clear();
        _cachedDisguiseSprites.Clear();
        _entityDisguiseIndex.Clear();

        for (var i = 0; i < disguises.Count; i++)
        {
            var disguise = disguises[i];
            var state = _sprite.GetState(disguise.Sprite);

            _cachedDisguiseSprites.Add(disguise);
            _loadedDisguises.Add(new LoadedDisguise(state, disguise.Size));
        }

        _disguisesLoaded = _loadedDisguises.Count > 0;
    }

    private int GetDisguiseIndexForEntity(EntityUid uid, int disguiseCount)
    {
        if (_entityDisguiseIndex.TryGetValue(uid, out int index))
            return index;

        index = _random.Next(disguiseCount);
        _entityDisguiseIndex[uid] = index;
        return index;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalEntity;
        if (player is null || !_hysteriaQuery.TryGetComponent(player.Value, out var hysteria))
            return;

        if (!_disguisesLoaded)
            return;

        var disguiseCount = _loadedDisguises.Count;
        if (disguiseCount == 0)
            return;

        var preserveSourceThrallVisibility =
            _thrallQuery.TryGetComponent(player.Value, out var playerThrall)
            && playerThrall.Master == hysteria.Source;

        var worldHandle = args.WorldHandle;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var worldBounds = args.WorldBounds.Enlarged(ViewBoundsMargin);

        // Query all humanoids
        var query = _entManager.EntityQueryEnumerator<HumanoidAppearanceComponent, TransformComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var xform, out var sprite))
        {
            if (xform.MapID != args.MapId) // Skip if not on the same map
                continue;

            if (uid == player) // Skip self
                continue;

            if (!sprite.Visible) // Skip entities that are not visible
                continue;

            // Skip thralls of the source vampire
            if (preserveSourceThrallVisibility
                && _thrallQuery.TryGetComponent(uid, out var thrall)
                && thrall.Master == hysteria.Source)
            {
                continue;
            }

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);

            // Check if in viewport bounds (with some margin)
            if (!worldBounds.Contains(worldPos))
                continue;

            // Get random sprite for this entity
            var disguiseIndex = GetDisguiseIndexForEntity(uid, disguiseCount);
            if (disguiseIndex >= disguiseCount)
                continue;

            var disguise = _loadedDisguises[disguiseIndex];

            // Get the direction from the targets sprite to match their facing
            var rsiDir = worldRot.ToRsiDirection(disguise.State.RsiDirections);
            var texture = disguise.State.GetFrame(rsiDir, 0);

            if (texture is null)
                continue;

            var angle = (worldRot + eyeRotation).Reduced().FlipPositive();
            var cardinal = !sprite.NoRotation && sprite.SnapCardinals
                ? angle.RoundToCardinalAngle()
                : Angle.Zero;

            var entityMatrix = Matrix3Helpers.CreateTransform(
                worldPos,
                sprite.NoRotation ? -eyeRotation : worldRot - cardinal);

            var spriteMatrix = Matrix3x2.Multiply(sprite.LocalMatrix, entityMatrix);

            worldHandle.SetTransform(spriteMatrix);
            worldHandle.DrawTextureRect(texture, Box2.FromDimensions(disguise.Size / -2f, disguise.Size));
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
    }

    private readonly record struct LoadedDisguise(RSI.State State, Vector2 Size);
}
