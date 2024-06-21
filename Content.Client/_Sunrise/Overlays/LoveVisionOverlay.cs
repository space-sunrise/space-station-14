using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Abilities;
using System.Numerics;

namespace Content.Client._Sunrise.Overlays;

public sealed class LoveVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] IEntityManager _entityManager = default!;
    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _loveVisionShader;

    public LoveVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _loveVisionShader = _prototypeManager.Index<ShaderPrototype>("LoveVision").Instance().Duplicate();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;
        if (_playerManager.LocalPlayer?.ControlledEntity is not { Valid: true } player)
            return;
        if (!_entityManager.HasComponent<LoveVisionComponent>(player))
            return;
        _loveVisionShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_loveVisionShader);
        worldHandle.DrawRect(viewport, Color.White);
    }
}
