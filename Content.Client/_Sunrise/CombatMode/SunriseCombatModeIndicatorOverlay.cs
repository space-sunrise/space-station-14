using System.Numerics;
using Content.Client.CombatMode;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.CombatMode;

public sealed class SunriseCombatModeIndicatorOverlay : Overlay
{
    private static readonly ResPath IndicatorRsi = new("/Textures/_Sunrise/Interface/CombatMode/combat_mode.rsi");
    private static readonly RSI.StateId IndicatorState = new("icon");
    private static readonly UIBox2 IndicatorRegion =
        UIBox2.FromDimensions(new Vector2(1f, 1f), new Vector2(5f, 5f));
    private const float HeadGap = 2f / EyeManager.PixelsPerMeter;
    private const float IndicatorSize = 6f / EyeManager.PixelsPerMeter;
    private static readonly Vector2 IndicatorOffset =
        new(-11f / EyeManager.PixelsPerMeter, -14f / EyeManager.PixelsPerMeter);

    private readonly IEntityManager _entity;
    private readonly IPlayerManager _player;
    private readonly CombatModeSystem _combatMode;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly Texture[] _frames;
    private readonly float[] _frameDelays;

    private float _frameTime;
    private int _frameIndex;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public SunriseCombatModeIndicatorOverlay(
        IEntityManager entity,
        IPlayerManager player,
        CombatModeSystem combatMode,
        IResourceCache resources)
    {
        _entity = entity;
        _player = player;
        _combatMode = combatMode;
        _sprite = entity.System<SpriteSystem>();
        _transform = entity.System<TransformSystem>();

        var rsi = resources.GetResource<RSIResource>(IndicatorRsi).RSI;
        if (!rsi.TryGetState(IndicatorState, out var state))
            throw new InvalidOperationException($"RSI {IndicatorRsi} has no state {IndicatorState}.");

        _frames = state.GetFrames(RsiDirection.South);
        _frameDelays = state.GetDelays();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _combatMode.IsInCombatMode() &&
               _player.LocalEntity != null &&
               base.BeforeDraw(in args);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_frames.Length <= 1)
            return;

        _frameTime += args.DeltaSeconds;
        while (_frameTime >= _frameDelays[_frameIndex])
        {
            _frameTime -= _frameDelays[_frameIndex];
            _frameIndex = (_frameIndex + 1) % _frames.Length;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player ||
            !_entity.TryGetComponent(player, out TransformComponent? xform) ||
            xform.MapID != args.MapId)
        {
            return;
        }

        var halfHeight = 0.5f;
        if (_entity.TryGetComponent(player, out SpriteComponent? sprite))
            halfHeight = _sprite.GetLocalBounds((player, sprite)).Height / 2f;

        var eyeRotation = args.Viewport.Eye?.Rotation ?? default;
        var headOffset = (-eyeRotation).ToWorldVec() *
                         -(halfHeight + IndicatorSize / 2f + HeadGap);
        var worldPosition = _transform.GetWorldPosition(xform) + headOffset;

        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRotation);
        var positionMatrix = Matrix3Helpers.CreateTranslation(worldPosition);
        args.WorldHandle.SetTransform(Matrix3x2.Multiply(rotationMatrix, positionMatrix));

        var frame = _frames[_frameIndex];
        var size = Vector2.One * IndicatorSize;
        args.WorldHandle.DrawTextureRectRegion(
            frame,
            Box2.CenteredAround(IndicatorOffset, size),
            subRegion: IndicatorRegion);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
