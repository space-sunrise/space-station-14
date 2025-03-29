using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.BloodCult.UI.TeleportRunesList;

public sealed class TeleportRunesListWindowBUI : BoundUserInterface
{
    private TeleportRunesListWindow? _window;

    public TeleportRunesListWindowBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TeleportRunesListWindow>();

        _window.ItemSelected += (item, index) =>
        {
            var msg = new TeleportRunesListWindowItemSelectedMessage(item, index);
            SendMessage(msg);
            _window.Close();
        };

        if (State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is TeleportRunesListWindowBUIState newState)
        {
            _window?.PopulateList(newState.Items, newState.Label);
        }
    }
}
