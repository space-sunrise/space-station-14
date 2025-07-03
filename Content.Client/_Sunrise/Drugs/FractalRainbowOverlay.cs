using Content.Shared.CCVar;
using Content.Shared.Drugs;
using Content.Shared.StatusEffect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Drugs;

public sealed class FractalRainbowOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private float _currentIntensity;
    private float _targetIntensity;
    private const float _interpolationSpeed = 1.5f;
    private const float _speed = 1.0f;

    public FractalRainbowOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("FractalRainbow").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var player = _playerManager.LocalEntity;
        if (player == null)
        {
            _targetIntensity = 0;
            return;
        }

        if (!_entityManager.HasComponent<SeeingFractalRainbowsComponent>(player)
            || !_entityManager.TryGetComponent<StatusEffectsComponent>(player, out var status))
        {
            _targetIntensity = 0;
            return;
        }

        var statusSys = _sysMan.GetEntitySystem<StatusEffectsSystem>();
        if (!statusSys.TryGetTime(player.Value, FractalRainbowOverlaySystem.EffectKey, out var time, status))
        {
            _targetIntensity = 0;
            return;
        }

        var timeLeft = (float)(time.Value.Item2 - time.Value.Item1).TotalSeconds;
        _targetIntensity = Math.Clamp(timeLeft / 5f, 0f, 1f);

        _currentIntensity = MathHelper.Lerp(
            _currentIntensity,
            _targetIntensity,
            _interpolationSpeed * args.DeltaSeconds
        );
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye
            && _currentIntensity > 0.01f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null) return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", _currentIntensity);
        _shader.SetParameter("speed", _speed);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    public void Reset()
    {
        _currentIntensity = 0;
        _targetIntensity = 0;
    }
}
