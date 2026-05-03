using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Radio;

namespace Content.Client._Sunrise.Radio.Ui;

public sealed class HeadsetBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _ent = default!;

    private HeadsetSettingsWindow? _window;

    public HeadsetBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new HeadsetSettingsWindow();
        _window.OnClose += Close;
        _window.OnChannelToggled += (id, enabled) => SendMessage(new HeadsetToggleChannelMessage(id, enabled));
        _window.OnVolumeChanged += (id, volume) => SendMessage(new HeadsetChangeVolumeMessage(id, volume));

        _window.OpenCentered();
        UpdateState();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        UpdateState();
    }

    private void UpdateState()
    {
        if (_window == null)
            return;

        if (!_ent.TryGetComponent<HeadsetComponent>(Owner, out var component))
            return;

        _ent.TryGetComponent<EncryptionKeyHolderComponent>(Owner, out var keys);
        _window.UpdateState(component, keys, _proto);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
