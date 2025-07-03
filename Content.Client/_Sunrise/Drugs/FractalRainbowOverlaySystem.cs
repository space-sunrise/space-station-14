using Content.Shared.Drugs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Drugs;

public sealed class FractalRainbowOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private FractalRainbowOverlay _overlay = default!;

    public static string EffectKey = "SeeingFractalRainbows";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeeingFractalRainbowsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SeeingFractalRainbowsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SeeingFractalRainbowsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SeeingFractalRainbowsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, SeeingFractalRainbowsComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, SeeingFractalRainbowsComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Reset();
    }

    private void OnInit(EntityUid uid, SeeingFractalRainbowsComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnShutdown(EntityUid uid, SeeingFractalRainbowsComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlay.Reset();
        }
    }
}
