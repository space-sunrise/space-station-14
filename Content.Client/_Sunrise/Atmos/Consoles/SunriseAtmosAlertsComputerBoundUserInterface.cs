using Content.Shared.Atmos.Components;

namespace Content.Client._Sunrise.Atmos.Consoles;

public sealed class SunriseAtmosAlertsComputerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SunriseAtmosAlertsComputerWindow? _menu;

    public SunriseAtmosAlertsComputerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new SunriseAtmosAlertsComputerWindow(this, Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (AtmosAlertsComputerBoundInterfaceState)state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
        _menu?.UpdateUI(xform?.Coordinates, castState.AirAlarms, castState.FireAlarms, castState.FocusData);
        _menu?.UpdateAlertSoundToggle(castState.DoAtmosAlert);
    }

    public void SendFocusChangeMessage(NetEntity? netEntity)
    {
        SendMessage(new AtmosAlertsComputerFocusChangeMessage(netEntity));
    }

    public void SendDeviceSilencedMessage(NetEntity netEntity, bool silenceDevice)
    {
        SendMessage(new AtmosAlertsComputerDeviceSilencedMessage(netEntity, silenceDevice));
    }

    public void SendAlertSoundToggleMessage(bool enabled)
    {
        SendMessage(new AtmosAlertsComputerAlertSoundToggleMessage(enabled));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu != null)
        {
            _menu.OnClose -= Close;
            _menu.Close();
            _menu = null;
        }
    }
}
