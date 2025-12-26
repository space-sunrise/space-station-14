using Content.Client.Lobby;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.Roadmap;

public sealed class RoadmapUIController : UIController, IOnStateEntered<LobbyState>
{
    private Roadmap? _window;
    private bool _shown;

    public void OnStateEntered(LobbyState state)
    {
        if (_shown || _window != null)
            return;

        ToggleRoadmap();
    }

    public void ToggleRoadmap()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        _shown = true;
        _window = new Roadmap();
        _window.OnClose += () => _window = null;

        _window.OpenCentered();
    }
}
