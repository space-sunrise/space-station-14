using Content.Shared.Synth.Components;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.Synth;

[UsedImplicitly]
public sealed class SynthMonitorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SynthMonitorMenu? _menu;

    public SynthMonitorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new SynthMonitorMenu();
        _menu.OnClose += Close;
        _menu.OnIdSelected += OnIdSelected;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not SynthScreenBoundUserInterfaceState st)
            return;

        _menu?.UpdateState(st.ScreenList);
    }

    private void OnIdSelected(string selectedId)
    {
        SendMessage(new SynthScreenPrototypeSelectedMessage(selectedId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _menu?.Close();
            _menu = null;
        }
    }
}
