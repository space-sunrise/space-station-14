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
    [Dependency] private readonly IPrototypeManager _prototype = default!;

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

        // if (!EntMan.TryGetComponent<CentComConsoleComponent>(_owner, out var centComConsoleComponent) &&
        //     centComConsoleComponent?.Station != null)
        // {
        //
        // }
        _window.IdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(IdCardSlotId));

        _window.EmergencyShuttleButton.OnPressed += EmergencyShuttleButtonOnOnPressed;

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    private void EmergencyShuttleButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
    {
        if (_window?.LastTime == null)
            SendMessage(new CentComConsoleCallEmergencyShuttleMessage(TimeSpan.FromMinutes(10))); // Тут ты закончил в прошлый раз
        else
            SendMessage(new CentComConsoleRecallEmergencyShuttleMessage());
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
