using Content.Client._Sunrise.MentorHelp;
using Content.Client.Administration.Systems;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._Sunrise.HelpChoice;

[UsedImplicitly]
public sealed class HelpChoiceUIController: UIController, IOnSystemChanged<MentorHelpSystem>
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _input = default!;

    private HelpChoiceWindow? _dialog;

    public void OnSystemLoaded(MentorHelpSystem system)
    {
        _input.SetInputCommand(ContentKeyFunctions.OpenHelpChoice,
            InputCmdHandler.FromDelegate(_ => ShowHelpChoiceDialog()));
    }

    public void OnSystemUnloaded(MentorHelpSystem system)
    {
        _input.SetInputCommand(ContentKeyFunctions.OpenHelpChoice, null);
    }

    private void ShowHelpChoiceDialog()
    {
        if (_dialog != null && _dialog.IsOpen)
        {
            _dialog.Close();
            _dialog = null;
            return;
        }

        if (_dialog == null)
        {
            _dialog = new HelpChoiceWindow();

            var local = _dialog;

            local.AHelpButton.OnPressed += _ =>
            {
                local.Close();
                _uiManager.GetUIController<AHelpUIController>().Open();
            };

            local.MentorHelpButton.OnPressed += _ =>
            {
                local.Close();
                _uiManager.GetUIController<MentorHelpUIController>().Open();
            };

            _dialog.OnClose += () => _dialog = null;
        }

        _dialog.OpenCentered();
    }
}
