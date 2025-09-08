using Content.Shared._Sunrise.CopyMachine;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.CopyMachine;

public sealed class CopyMachineBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CopyMachineMenu? _window;

    public CopyMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CopyMachineMenu>();

        _window.OnPrintPressed += templateId =>
        {
            if (templateId != null)
                SendMessage(new CopyMachinePrintMessage(templateId));
        };

        _window.OnCopyPressed += () =>
        {
            SendMessage(new CopyMachineCopyMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CopyMachineBoundUserInterfaceState s || _window == null)
            return;

        _window.UpdateState(s);
    }
}
