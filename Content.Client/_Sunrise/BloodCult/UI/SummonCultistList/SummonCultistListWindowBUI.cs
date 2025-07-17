using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.BloodCult.UI.SummonCultistList;

public sealed class SummonCultistListWindowBUI : BoundUserInterface
{
    private SummonCultistListWindow? _window;

    public SummonCultistListWindowBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SummonCultistListWindow>();
        _window.OnClose += Close;

        _window.ItemSelected += (item, index) =>
        {
            var msg = new SummonCultistListWindowItemSelectedMessage(item, index);
            SendMessage(msg);
            _window.Close();
        };

        if (State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SummonCultistListWindowBUIState newState)
        {
            _window?.PopulateList(newState.Items, newState.Label, newState.MobStates, newState.Distances);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Close();
    }
}
