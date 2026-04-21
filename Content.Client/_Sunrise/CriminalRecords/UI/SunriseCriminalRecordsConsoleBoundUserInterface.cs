using Content.Shared._Sunrise.CriminalRecords;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.CriminalRecords.UI;

public sealed class SunriseCriminalRecordsConsoleBoundUserInterface : BoundUserInterface
{
    private SunriseCriminalRecordsWindow? _window;

    public SunriseCriminalRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SunriseCriminalRecordsWindow>();
        _window.Initialize(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SunriseCriminalRecordsConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    public void CreateCase()
    {
        SendMessage(new SunriseCriminalRecordsCreateCaseMessage());
    }

    public void UpdateCase(uint caseId, List<string> laws, List<string> circumstances, string? notes)
    {
        SendMessage(new SunriseCriminalRecordsUpdateCaseMessage(caseId, laws, circumstances, notes));
    }

    public void CloseCase(uint caseId)
    {
        SendMessage(new SunriseCriminalRecordsCloseCaseMessage(caseId));
    }

    public void SelectRecord(uint? recordId)
    {
        SendMessage(new SunriseCriminalRecordsSelectRecordMessage(recordId));
    }

    public void SelectCase(uint caseId)
    {
        SendMessage(new SunriseCriminalRecordsSelectCaseMessage(caseId));
    }

    public void SetUIState(SunriseCriminalRecordsUIState state)
    {
        SendMessage(new SunriseCriminalRecordsSetUIStateMessage(state));
    }

}
