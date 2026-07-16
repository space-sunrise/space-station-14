using Content.Client._Sunrise.Sponsor;
using Content.Client.Lobby;
using Content.Sunrise.Interfaces.Shared;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.Sponsor;

public sealed class SponsorWindowUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private SponsorWindow? _window;
    private bool _shown;

    public void OnStateEntered(LobbyState state)
    {
        IoCManager.Instance!.TryResolveType<ISharedSponsorsManager>(out var sponsors);

        if (_shown || sponsors == null || sponsors.ClientIsSponsor())
            return;

        OpenWindow();
        _shown = true;
    }

    public void OnStateExited(LobbyState state)
    {
        _window?.Close();
    }

    public void OpenWindow()
    {
        var window = EnsureWindow();

        window.OpenCentered();
        window.MoveToFront();
    }

    public void ToggleWindow()
    {
        var window = EnsureWindow();

        if (window.IsOpen)
            window.Close();
        else
            OpenWindow();
    }

    private SponsorWindow EnsureWindow()
    {
        if (_window is { Disposed: false })
            return _window;

        _window = _ui.CreateWindow<SponsorWindow>();
        return _window;
    }
}
