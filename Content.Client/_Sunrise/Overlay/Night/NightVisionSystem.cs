using Content.Client._Sunrise.ThermalVision;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Mech;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.Flash.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory.Events;

namespace Content.Client._Starlight.Overlay.Night;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (ent.Comp.Effect == null && !ent.Comp.blockedByFlashImmunity)
            AddNightVision(ent.Owner, ent.Comp);
    }

    private void OnPlayerDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveNightVision(ent.Comp);
    }

    private void OnVisionInit(Entity<NightVisionComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent.Owner) return;

        if (ent.Comp.Effect == null)
            AddNightVision(ent.Owner, ent.Comp);
    }

    private void OnVisionShutdown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner) return;
        RemoveNightVision(ent.Comp);
    }

    private void AddNightVision(EntityUid uid, NightVisionComponent component)
    {
        _overlayMan.AddOverlay(_overlay);
        component.Effect = SpawnAttachedTo(component.EffectPrototype, Transform(uid).Coordinates);
        _xformSys.SetParent(component.Effect.Value, uid);
    }

    private void RemoveNightVision(NightVisionComponent component)
    {
        _overlayMan.RemoveOverlay(_overlay);
        Del(component.Effect);
        component.Effect = null;
    }
}
