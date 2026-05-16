using Content.Client.UserInterface.Systems.Sandbox.Windows;

namespace Content.Client.UserInterface.Systems.Sandbox;

public partial class SandboxUIController
{
    public partial void OnThermalVisionChanged()
    {
        if (_window == null)
            return;

        _window.ThermalVisionButton.Pressed = _sandbox.ThermalVisionActive;
    }
}
