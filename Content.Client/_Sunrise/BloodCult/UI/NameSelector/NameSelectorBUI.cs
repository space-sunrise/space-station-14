using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.BloodCult.UI.NameSelector;

public sealed class NameSelectorBUI : BoundUserInterface
{
    private NameSelectorWindow? _window;

    public NameSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NameSelectorWindow>();
        _window.OnNameChange += OnNameSelected;
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new NameSelectorMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NameSelectorBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Name);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
