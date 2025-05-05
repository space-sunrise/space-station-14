using Content.Shared._Sunrise.BloodCult.Items;

namespace Content.Client._Sunrise.BloodCult.UI.Torch;

public sealed class TorchWindowBUI : BoundUserInterface
{
    private TorchWindow? _window;

    public TorchWindowBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.ItemSelected += (uid, item) =>
        {
            var msg = new TorchWindowItemSelectedMessage(uid, item);
            SendMessage(msg);
            _window.Close();
        };

        if (State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is TorchWindowBUIState newState)
        {
            _window?.PopulateList(newState.Items);
        }
    }
}
