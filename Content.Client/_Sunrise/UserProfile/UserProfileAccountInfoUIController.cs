using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.UserProfile;

[UsedImplicitly]
public sealed class UserProfileAccountInfoUIController : UIController
{
    private UserProfileAccountInfoWindow _window = default!;

    public void OpenWindow()
    {
        EnsureWindow();

        _window.OpenCentered();
        _window.MoveToFront();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<UserProfileAccountInfoWindow>();
    }
}
