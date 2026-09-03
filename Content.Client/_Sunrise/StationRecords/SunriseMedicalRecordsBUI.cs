using Content.Shared._Sunrise.StationRecords;
using Content.Shared.StationRecords;

namespace Content.Client._Sunrise.StationRecords;

public sealed class SunriseMedicalRecordsBoundUserInterface : BoundUserInterface
{
    private SunriseMedicalRecordsWindow? _window;

    public SunriseMedicalRecordsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new SunriseMedicalRecordsWindow();
        _window.OnKeySelected += key =>
            SendMessage(new SelectStationRecord(key));
        _window.OnFilterChanged += args =>
            SendMessage(new SetStationRecordFilter(args.Item1, args.Item2));
        _window.OnPrintPressed += id =>
            SendMessage(new SunrisePrintMedicalRecord(id));
        _window.OnSavePressed += (text, id) =>
            SendMessage(new SunriseSaveMedicalRecord(text, id));
        _window.OnClose += Close;

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SunriseMedicalRecordsConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
    }
}
