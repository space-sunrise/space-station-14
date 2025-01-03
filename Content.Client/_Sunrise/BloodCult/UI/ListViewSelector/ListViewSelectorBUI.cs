using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.BloodCult.UI.ListViewSelector;


public sealed class ListViewSelectorBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private ListViewSelectorWindow? _window;

    public ListViewSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new ListViewSelectorWindow(_prototypeManager, _entityManager);
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.ItemSelected += (item, index) =>
        {
            var msg = new ListViewItemSelectedMessage(item, index);
            SendMessage(msg);
        };

        if(State != null)
            UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ListViewBUIState newState)
        {
            _window?.PopulateList(newState.Items);
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
