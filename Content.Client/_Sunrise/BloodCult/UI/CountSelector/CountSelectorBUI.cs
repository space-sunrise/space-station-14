using Content.Shared._Sunrise.BloodCult.Items;

namespace Content.Client._Sunrise.BloodCult.UI.CountSelector;

public sealed class CountSelectorBUI : BoundUserInterface
{
    private CountSelectorWindow? _window;

    public CountSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OpenCentered();
        _window.OnCountChange += OnNameSelected;
        _window.OnClose += Close;
    }

    private void OnNameSelected(string name)
    {
        if (int.TryParse(name, out var count) && count >= 50)
        {
            SendMessage(new CountSelectorMessage(count));
            Close();
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CountSelectorBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Count);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
