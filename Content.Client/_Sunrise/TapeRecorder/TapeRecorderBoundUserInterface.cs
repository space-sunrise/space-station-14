using Content.Shared._Sunrise.TapeRecorder;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.TapeRecorder;

public sealed class TapeRecorderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private TapeRecorderMenu? _menu;

    protected override void Open()
    {
        if (IsOpened)
            return;

        base.Open();

        _menu = this.CreateWindow<TapeRecorderMenu>();
        _menu.OnModePressed += OnModePressed;
        _menu.OnPrintPressed += OnPrintPressed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not TapeRecorderBoundUserInterfaceState recorderState)
            return;

        _menu?.UpdateState(recorderState);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _menu != null)
        {
            _menu.OnModePressed -= OnModePressed;
            _menu.OnPrintPressed -= OnPrintPressed;
            _menu = null;
        }

        base.Dispose(disposing);
    }

    private void OnModePressed(TapeRecorderMode mode)
    {
        SendMessage(new TapeRecorderSetModeMessage(mode));
    }

    private void OnPrintPressed()
    {
        SendMessage(new TapeRecorderPrintMessage());
    }
}
