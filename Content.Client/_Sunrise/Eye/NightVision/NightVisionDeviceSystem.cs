using Content.Client.Overlays;
using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared._Sunrise.Eye.NightVision.Systems;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Eye.NightVision;

public sealed class NightVisionDeviceOverlaySystem : EquipmentHudSystem<NightVisionDeviceComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private NightVisionDeviceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
        SubscribeLocalEvent<NightVisionDeviceComponent, NightVisionDeviceToggledEvent>(OnNightVisionToggled);
    }

    private void OnNightVisionToggled(EntityUid uid, NightVisionDeviceComponent component, NightVisionDeviceToggledEvent args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;
        if (playerEntity == null)
            return;

        if (playerEntity == args.Equipped)
        {
            _overlay.Enabled = component.Activated;
            // Явный бред
            _lightManager.DrawLighting = !component.Activated;
        }
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionDeviceComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var comp in component.Components)
        {
            if (_prototypeManager.TryIndex<ShaderPrototype>(comp.DisplayShader, out var shaderPrototype))
                _overlay.Shader = shaderPrototype.InstanceUnique();
            _overlay.DisplayColor = comp.DisplayColor;
            _overlay.Enabled = comp.Activated;
        }
        if (!_overlayMan.HasOverlay<NightVisionOverlay>())
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayMan.RemoveOverlay(_overlay);
        // Явный бред
        _lightManager.DrawLighting = true;
    }
}
