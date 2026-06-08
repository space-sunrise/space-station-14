using Content.Shared._Sunrise.TapeRecorder;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.TapeRecorder;

public sealed class TapeRecorderBoundUserInterface : BoundUserInterface
{
    private TapeRecorderMenu? _menu;

    public TapeRecorderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TapeRecorderMenu>();
        _menu.OnModePressed += mode => SendMessage(new TapeRecorderSetModeMessage(mode));
        _menu.OnPrintPressed += () => SendMessage(new TapeRecorderPrintMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is TapeRecorderBoundUserInterfaceState recorderState)
            _menu?.UpdateState(recorderState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _menu == null)
            return;

        _menu.OnModePressed = null;
        _menu.OnPrintPressed = null;
        _menu = null;
    }
}
