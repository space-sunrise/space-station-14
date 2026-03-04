using Content.Shared._Sunrise.SunriseCCVars; // Sunrise-Edit
using Content.Shared._Sunrise.TTS; // Sunrise-Edit
using Content.Shared.VoiceMask;
using Content.Client._Sunrise.TTS; // Sunrise-Edit
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
        IoCManager.InjectDependencies(this); // Sunrise-Edit
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VoiceMaskNameChangeWindow>();
        _window.ReloadVerbs(_protomanager);
        _window.AddVerbs();
        _window.LoadVoiceList(IoCManager.Resolve<IPrototypeManager>()); // Sunrise-Edit

        _window.OnNameChange += OnNameSelected;
        _window.OnVerbChange += verb => SendMessage(new VoiceMaskChangeVerbMessage(verb));
        _window.OnVoiceSelected += OnVoiceSelected; // Sunrise-Edit
        _window.OnVoicePreview += OnVoicePreview; // Sunrise-Edit
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new VoiceMaskChangeNameMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state); // Sunrise-Edit
        if (state is not VoiceMaskBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Name, cast.Voice, cast.Verb); // Sunrise-Edit
    }

    // Sunrise-Start
    private void OnVoiceSelected(string voiceId)
    {
        SendMessage(new VoiceMaskChangeVoiceMessage(voiceId));
    }
    private void OnVoicePreview(string voiceId)
    {
        EntMan.System<TTSSystem>().RequestPreviewTts(voiceId);
    }
    // Sunrise-End

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
