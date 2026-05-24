using Content.Shared._Sunrise.CriminalRecords;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

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
        _window = this.CreateWindow<PrisonerManagementConsoleWindow>();
        _window.OnStartIncarceration += StartIncarceration;
        _window.OnEscape += Escape;
        _window.OnParole += Parole;
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

    public void Escape(uint recordId, uint caseId)
    {
        SendMessage(new PrisonerManagementEscapeMessage(recordId, caseId));
    }

    public void Parole(uint recordId, uint caseId)
    {
        SendMessage(new PrisonerManagementParoleMessage(recordId, caseId));
    }
}
