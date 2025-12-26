using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.VoiceMask;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.VoiceMask;

public sealed class VoiceMaskBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private VoiceMaskNameChangeWindow? _window;

    public VoiceMaskBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VoiceMaskNameChangeWindow>();
        _window.ReloadVerbs(_protomanager);
        _window.AddVerbs();
        // Sunrise-Start
        if (IoCManager.Resolve<IConfigurationManager>().GetCVar(SunriseCCVars.TTSEnabled))
        {
            _window.ReloadVoices(IoCManager.Resolve<IPrototypeManager>());
        }
        // Sunrise-End

        _window.OnNameChange += OnNameSelected;
        _window.OnVerbChange += verb => SendMessage(new VoiceMaskChangeVerbMessage(verb));
        _window.OnVoiceChange += voice => SendMessage(new VoiceMaskChangeVoiceMessage(voice)); // Sunrise-Edit
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new VoiceMaskChangeNameMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not VoiceMaskBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Name, cast.Voice, cast.Verb); // Sunrise-Edit
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
