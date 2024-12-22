using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.SponsorTiers;

public partial class SponsorTiersUIController : UIController
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private SponsorTiersUi _sponsorTiersUi = default!;

    public void OpenWindow()
    {
        EnsureWindow();

        _sponsorTiersUi.OpenCentered();
        _sponsorTiersUi.MoveToFront();
    }

    private void EnsureWindow()
    {
        if (_sponsorTiersUi is { Disposed: false })
            return;

        _sponsorTiersUi = _uiManager.CreateWindow<SponsorTiersUi>();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_sponsorTiersUi.IsOpen)
        {
            _sponsorTiersUi.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
