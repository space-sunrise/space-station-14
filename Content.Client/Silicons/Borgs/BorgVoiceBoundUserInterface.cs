using Content.Client._Sunrise.TTS;
using Content.Shared._Sunrise.TTS;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.Borgs;

public sealed class BorgVoiceBoundUserInterface : BoundUserInterface
{
    private BorgVoiceWindow? _window;

    public BorgVoiceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BorgVoiceWindow>();
        _window.OnVoiceSelected += OnVoiceSelected;
        _window.OnVoicePreview += OnVoicePreview;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BorgVoiceChangeState borgState || _window == null)
            return;

        _window.UpdateState(borgState);
    }

    private void OnVoiceSelected(string voiceId)
    {
        SendMessage(new BorgVoiceChangeMessage(voiceId));
    }

    private void OnVoicePreview(string voiceId)
    {
        EntMan.System<TTSSystem>().RequestPreviewTts(voiceId);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
