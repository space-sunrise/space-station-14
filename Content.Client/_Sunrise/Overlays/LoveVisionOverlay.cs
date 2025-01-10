using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Aphrodesiac;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.StatusEffect;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.LoveVision;

public sealed class LoveVisionOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] IEntityManager _entityManager = default!;

    public override bool RequestScreenTexture => true;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _loveVisionShader;

    private float _strength = 0.0f;
    private float _timeTicker = 0.0f;

    private readonly float _maxStrength = 15f;
    private readonly float _minStrength = 0f;

    // private float EffectScale => Math.Clamp((_strength - _minStrength) / _maxStrength, 0.0f, 1.0f);

    public LoveVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _loveVisionShader = _prototypeManager.Index<ShaderPrototype>("LoveVision").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.HasComponent<LoveVisionComponent>(playerEntity)
            || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
            return;

        var statusSys = _sysMan.GetEntitySystem<StatusEffectsSystem>();
        if (!statusSys.TryGetTime(playerEntity.Value, LoveVisionSystem.LoveVisionKey, out var time, status))
            return;

        var duration = (float)(time.Value.Item2 - time.Value.Item1).TotalSeconds;
        var elapsedTime = _timeTicker;
        _timeTicker += args.DeltaSeconds;

        var halfDuration = duration / 2f;
        var normalizedTime = MathF.Abs(elapsedTime - halfDuration) / halfDuration; // 0 at the middle, 1 at the ends

        // Invert the normalized time to make it peak in the middle
        var peakFactor = 1f - normalizedTime;

        // Adjust strength based on peakFactor
        _strength += (peakFactor * args.DeltaSeconds);

        // Optional: Clamp _strength to a reasonable range if needed, e.g., between 0 and 1
        _strength = Math.Clamp(_strength, 0f, 1f);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComponent))
            return false;

        if (args.Viewport.Eye != eyeComponent.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_config.GetCVar(CCVars.ReducedMotion))
            return;

        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        _loveVisionShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _loveVisionShader?.SetParameter("effectStrength", _strength);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_loveVisionShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
