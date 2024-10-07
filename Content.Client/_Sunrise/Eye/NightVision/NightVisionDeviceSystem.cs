using Content.Client.Overlays;
using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared._Sunrise.Eye.NightVision.Systems;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Eye.NightVision;

public sealed class NightVisionDeviceOverlaySystem : EquipmentHudSystem<NightVisionDeviceComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private NightVisionDeviceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
        SubscribeLocalEvent<NightVisionDeviceComponent, NightVisionDeviceToggledEvent>(OnNightVisionToggled);
    }

    private void OnNightVisionToggled(EntityUid uid, NightVisionDeviceComponent component, NightVisionDeviceToggledEvent args)
    {
        _overlay.Enabled = args.Enabled;
        _lightManager.DrawLighting = !args.Enabled;
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionDeviceComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var comp in component.Components)
        {
            Logger.Info($"UpdateInternal");
            Logger.Info($"DisplayShader: {comp.DisplayShader}");
            if (_prototypeManager.TryIndex<ShaderPrototype>(comp.DisplayShader, out var shaderPrototype))
                _overlay.Shader = shaderPrototype.InstanceUnique();
            Logger.Info($"DisplayColor: {comp.DisplayColor}");
            _overlay.DisplayColor = comp.DisplayColor;
            Logger.Info($"Enabled: {comp.Activated}");
            _overlay.Enabled = comp.Activated;
            _lightManager.DrawLighting = !comp.Activated;
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
        _lightManager.DrawLighting = true;
    }
}
