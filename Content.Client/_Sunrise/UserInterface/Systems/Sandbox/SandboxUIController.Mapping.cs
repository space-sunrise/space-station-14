using Content.Client._Sunrise.Sandbox;
using Robust.Client.UserInterface;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.UserInterface.Systems.Sandbox;

public sealed partial class SandboxUIController
{
    [UISystemDependency] private readonly MappingAccessOverlaySystem _mappingAccess = default!;
    [UISystemDependency] private readonly MappingTransparencySystem _mappingTransparency = default!;

    partial void InitializeSunriseWindow()
    {
        if (_window == null)
            return;

        _window.ToggleMappingAccessButton.Visible = _mappingAccess.CanEnable;
        _window.ToggleMappingAccessButton.Pressed = _mappingAccess.Enabled;
        _window.ToggleMappingAccessButton.OnPressed += _ => _mappingAccess.TrySetEnabled(!_mappingAccess.Enabled);

        _window.ToggleMappingTransparencyButton.Visible = _mappingTransparency.CanEnable;
        _window.ToggleMappingTransparencyButton.Pressed = _mappingTransparency.Enabled;
        _window.ToggleMappingTransparencyButton.OnPressed += _ => _sandbox.ToggleMappingTransparency();
    }

    public void SetToggleMappingAccess(bool value)
    {
        if (_window == null)
            return;

        _window.ToggleMappingAccessButton.Pressed = value;
    }

    public void SetMappingAccessVisible(bool value)
    {
        if (_window == null)
            return;

        _window.ToggleMappingAccessButton.Visible = value;
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
