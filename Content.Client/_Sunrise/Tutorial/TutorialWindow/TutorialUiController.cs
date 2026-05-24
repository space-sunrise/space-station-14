using Content.Client.Lobby;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Tutorial.TutorialWindow;

public sealed class TutorialUIController : UIController,
    IOnStateEntered<LobbyState>,
    IOnStateExited<LobbyState>,
    IOnSystemChanged<TutorialSystem>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TutorialSystem? _tutorialSystem;
    private TutorialSystem? _windowDataSystem;
    private TutorialWindow? _window;
    private Action<TutorialSequencePrototype>? _startTutorialHandler;
    private Action? _requestCompletedTutorialsHandler;
    private bool _shown;
    private bool _autoOpenEnabled = true;

    public void OnSystemLoaded(TutorialSystem system)
    {
        _tutorialSystem = system;
    }

    public void OnSystemUnloaded(TutorialSystem system)
    {
        _window?.Close();
        UnsubscribeWindowData();

        if (_tutorialSystem == system)
            _tutorialSystem = null;
    }

    public void ToggleTutorial()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        var tutorialSystem = _tutorialSystem;
        if (tutorialSystem == null)
            return;

        _shown = true;
        _window = UIManager.CreateWindow<TutorialWindow>();

        if (tutorialSystem.CompletedTutorialsReceived)
            _window.SetCompletedTutorials(tutorialSystem.CompletedTutorials);

        SubscribeWindowData(tutorialSystem);

        _startTutorialHandler = proto =>
            tutorialSystem.RequestStartTutorial(new ProtoId<TutorialSequencePrototype>(proto.ID));
        _requestCompletedTutorialsHandler = tutorialSystem.RequestWindowData;

        _window.OnTutorialButtonPressed += _startTutorialHandler;
        _window.OnRequestCompletedTutorials += _requestCompletedTutorialsHandler;
        _window.OnClose += OnWindowClosed;

        _window.OpenCentered();
    }

    private void TryOpenTutorial()
    {
        if (_shown || _window != null)
            return;

        ToggleTutorial();
    }
    public void OnStateEntered(LobbyState state)
    {
        _cfg.OnValueChanged(SunriseCCVars.TutorialWindowAutoOpen, OnAutoOpenChanged, true);

        if (!_autoOpenEnabled)
            return;

        var tutorialSystem = _tutorialSystem;
        if (tutorialSystem == null)
            return;

        SubscribeWindowData(tutorialSystem);

        if (!tutorialSystem.CompletedTutorialsReceived)
            tutorialSystem.RequestWindowData();

        TryOpenTutorial();
    }

    public void OnStateExited(LobbyState state)
    {
        _cfg.UnsubValueChanged(SunriseCCVars.TutorialWindowAutoOpen, OnAutoOpenChanged);

        _window?.Close();
        UnsubscribeWindowData();
    }

    private void OnAutoOpenChanged(bool value)
    {
        _autoOpenEnabled = value;
    }

    private void OnWindowDataReceived()
    {
        var tutorialSystem = _windowDataSystem;
        if (tutorialSystem == null)
            return;

        if (tutorialSystem.CompletedTutorialsReceived)
            _window?.SetCompletedTutorials(tutorialSystem.CompletedTutorials);

        TryOpenTutorial();
    }

    private void OnWindowClosed()
    {
        var window = _window;
        if (window == null)
            return;

        if (_startTutorialHandler != null)
            window.OnTutorialButtonPressed -= _startTutorialHandler;

        if (_requestCompletedTutorialsHandler != null)
            window.OnRequestCompletedTutorials -= _requestCompletedTutorialsHandler;

        window.OnClose -= OnWindowClosed;

        _startTutorialHandler = null;
        _requestCompletedTutorialsHandler = null;
        _window = null;
    }

    private void SubscribeWindowData(TutorialSystem system)
    {
        if (_windowDataSystem == system)
            return;

        UnsubscribeWindowData();

        system.WindowDataReceived += OnWindowDataReceived;
        _windowDataSystem = system;
    }

    private void UnsubscribeWindowData()
    {
        if (_windowDataSystem == null)
            return;

        _windowDataSystem.WindowDataReceived -= OnWindowDataReceived;
        _windowDataSystem = null;
    }
}
