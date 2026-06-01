using Content.Shared._Sunrise.Guardian;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.Guardian.UI;

[UsedImplicitly]
public sealed class GuardianCreatorSelectorBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private GuardianCreatorSelectorWindow? _window;


    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GuardianCreatorSelectorWindow>();
        _window.Confirmed += OnConfirmed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not GuardianCreatorSelectorBuiState selectorState)
            return;

        _window.UpdateState(selectorState);
    }

    private void OnConfirmed(string prototype)
    {
        SendMessage(new GuardianCreatorSelectorConfirmMessage(prototype));
    }
}
