using Content.Shared._Sunrise.Eye.NightVision.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.Mobs;

namespace Content.Client._Sunrise.Eye.NightVision;
public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnNightVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnNightVisionShutdown);

        SubscribeLocalEvent<NightVisionComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NightVisionComponent, MobStateChangedEvent>(OnMobStateChanged);

        _overlay = new();
    }

    private void OnMobStateChanged(EntityUid uid, NightVisionComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ResetLighting();
        }
    }

    private void ResetLighting()
    {
        if (_overlayMan.HasOverlay<NightVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }

    private void OnPlayerAttached(EntityUid uid, NightVisionComponent component, PlayerAttachedEvent args)
    {
        if (_overlay == default!)
            return;
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, NightVisionComponent component, PlayerDetachedEvent args)
    {
        if (_overlay == default!)
            return;
        ResetLighting();
    }

    private void OnNightVisionInit(EntityUid uid, NightVisionComponent component, ComponentInit args)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnNightVisionShutdown(EntityUid uid, NightVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;
        if (_overlay == default!)
            return;
        ResetLighting();
    }
}
