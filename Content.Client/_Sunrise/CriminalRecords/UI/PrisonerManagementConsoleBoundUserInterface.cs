using Content.Shared._Sunrise.CriminalRecords;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.CriminalRecords.UI;

public sealed class PrisonerManagementConsoleBoundUserInterface : BoundUserInterface
{
    private PrisonerManagementConsoleWindow? _window;

    public PrisonerManagementConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new PrisonerManagementConsoleWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not PrisonerManagementConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    public void StartIncarceration(uint recordId, uint caseId, int cellIndex)
    {
        SendMessage(new PrisonerManagementStartIncarcerationMessage(recordId, caseId, cellIndex));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
