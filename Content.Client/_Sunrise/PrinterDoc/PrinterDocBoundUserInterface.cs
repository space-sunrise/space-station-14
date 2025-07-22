using Content.Shared._Sunrise.PrinterDoc;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.PrinterDoc;

public sealed class PrinterDocBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PrinterDocMenu? _window;

    public PrinterDocBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PrinterDocMenu>();

        _window.OnPrintPressed += templateId =>
        {
            if (templateId != null)
                SendMessage(new PrinterDocPrintMessage(templateId));
        };

        _window.OnCopyPressed += () =>
        {
            SendMessage(new PrinterDocCopyMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not PrinterDocBoundUserInterfaceState s || _window == null)
            return;

        _window.UpdateState(s);
    }
}
