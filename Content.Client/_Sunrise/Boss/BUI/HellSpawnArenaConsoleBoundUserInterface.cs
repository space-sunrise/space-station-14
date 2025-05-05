using Content.Client._Sunrise.Boss.UI;
using Content.Shared._Sunrise.Boss.Systems;

namespace Content.Client._Sunrise.Boss.BUI;

public sealed class HellSpawnArenaConsoleBoundUserInterface : BoundUserInterface
{
    private readonly IEntityManager _entManager;

    private readonly EntityUid _owner;
    private HellSpawnArenaConsoleWindow? _window;

    public HellSpawnArenaConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _entManager = IoCManager.Resolve<IEntityManager>();
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();
        _window = new HellSpawnArenaConsoleWindow();
        _window.OnClose += Close;

        _window.TravelButtonPressed += OnTravelButtonPressed;

        _window.OpenCentered();
    }

    private void OnTravelButtonPressed()
    {
        if (_window == null)
            return;

        var ev = new TravelButtonPressedMessage
        {
            Owner = _entManager.GetNetEntity(_owner),
        };
        SendMessage(ev);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not HellSpawnArenaConsoleUiState cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
}
