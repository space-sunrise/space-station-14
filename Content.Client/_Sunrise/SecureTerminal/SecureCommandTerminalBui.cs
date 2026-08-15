using Content.Shared._Sunrise.SecureTerminal;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.SecureTerminal;

/// <summary>
/// Connects the Secure Command Terminal window to its entity-bound interface.
/// </summary>
public sealed class SecureCommandTerminalBui : BoundUserInterface
{
    [ViewVariables]
    private SecureCommandTerminalWindow? _window;

    public SecureCommandTerminalBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SecureCommandTerminalWindow>();
        _window.OnRequest += requestId => SendMessage(new SecureTerminalRequestMessage(requestId));
        _window.OnAuthorize += requestId => SendMessage(new SecureTerminalAuthorizeMessage(requestId));
        _window.OnDeny += requestId => SendMessage(new SecureTerminalDenyMessage(requestId));
        _window.OnRecall += requestId => SendMessage(new SecureTerminalRecallMessage(requestId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is SecureCommandTerminalInterfaceState terminalState)
            _window?.UpdateState(terminalState);
    }
}
