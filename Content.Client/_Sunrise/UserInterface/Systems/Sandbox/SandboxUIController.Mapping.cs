using Content.Client._Sunrise.Sandbox;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers.Implementations;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.UserInterface.Systems.Sandbox;

public sealed partial class SandboxUIController
{
    [UISystemDependency] private readonly MappingTransparencySystem _mappingTransparency = default!;

    partial void InitializeSunriseWindow()
    {
        if (_window == null)
            return;

        _window.ToggleMappingTransparencyButton.Visible = _mappingTransparency.CanEnable;
        _window.ToggleMappingTransparencyButton.Pressed = _mappingTransparency.Enabled;
        _window.ToggleMappingTransparencyButton.OnPressed += _ => _sandbox.ToggleMappingTransparency();
    }

    public void SetToggleMappingTransparency(bool value)
    {
        if (_window == null)
            return;

        _window.ToggleMappingTransparencyButton.Pressed = value;
    }

    public void SetMappingTransparencyVisible(bool value)
    {
        if (_window == null)
            return;

        _window.ToggleMappingTransparencyButton.Visible = value;
    }
}
