using Content.Client._Sunrise.CentCom.UI;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.CentCom.BUI;

[UsedImplicitly]
public sealed class CentComConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private CentComConsoleWindow? _window;

    public CentComConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<CentComConsoleComponent>(Owner, out var centComConsoleComponent))
        {
            Dispose();
            return;
        }

        if (centComConsoleComponent.Station == null)
            return;

        _window = this.CreateWindow<CentComConsoleWindow>();
        _window.OnClose += Close;
        _window.OpenCentered();
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
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Dispose();
    }
}
