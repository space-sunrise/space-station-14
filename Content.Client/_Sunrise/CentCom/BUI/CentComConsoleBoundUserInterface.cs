using Content.Client._Sunrise.CentCom.UI;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Content.Shared._Sunrise.CentCom.CentComConsoleComponent;

namespace Content.Client._Sunrise.CentCom.BUI;

[UsedImplicitly]
public sealed class CentComConsoleBoundUserInterface : BoundUserInterface
{

    private CentComConsoleWindow? _window;
    private EntityUid? _owner;

    public CentComConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CentComConsoleWindow>();

        _window.IdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(IdCardSlotId));

        _window.OnEmergencyShuttle += args => EmergencyShuttleButtonOnOnPressed(args);

        _window.OnClose += Close;
        _window.OnAnnounce += s => SendMessage(new CentComConsoleAnnounceMessage(s));
        _window.OnAlertLevel += s => SendMessage(new CentComConsoleAlertLevelChangeMessage(s));
        _window.OpenCentered();
    }

    private void EmergencyShuttleButtonOnOnPressed(object? args)
    {
        if (args == null)
            return;
        if (_window?.LastTime == null)
        {
            SendMessage(args.ToString() == "10 минут"
                ? new CentComConsoleCallEmergencyShuttleMessage(TimeSpan.FromMinutes(10))
                : new CentComConsoleCallEmergencyShuttleMessage(TimeSpan.FromMinutes(5)));
        }
        else
        {
            SendMessage(new CentComConsoleRecallEmergencyShuttleMessage());
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var commsState = state as CentComConsoleBoundUserInterfaceState;
        if (commsState == null)
            return;
        if (_window == null)
            return;

        _window.UpdateState(commsState);
        _owner = EntMan.GetEntity(commsState.Owner);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Dispose();
    }
}
