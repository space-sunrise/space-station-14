using Robust.Client.Graphics;

namespace Content.Client._Sunrise.Guardian;

public sealed class GuardianLightningArcSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new GuardianLightningArcOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<GuardianLightningArcOverlay>();
    }
}
