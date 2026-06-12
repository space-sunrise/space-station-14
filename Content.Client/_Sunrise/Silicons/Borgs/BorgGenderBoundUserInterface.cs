using Content.Shared._Sunrise.Silicons.Borgs;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Silicons.Borgs;

public sealed class BorgGenderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BorgGenderWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BorgGenderWindow>();
        _window.OnGenderSelected += OnGenderSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BorgGenderBuiState borgState || _window == null)
            return;

        _window.UpdateState(borgState);
    }

    private void OnGenderSelected(BorgGender gender)
    {
        SendPredictedMessage(new BorgGenderChangeMessage(gender));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();
    }
}
