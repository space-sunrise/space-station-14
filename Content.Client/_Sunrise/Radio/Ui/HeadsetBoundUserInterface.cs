using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Radio;

namespace Content.Client._Sunrise.Radio.Ui;

public sealed class HeadsetBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _ent = default!;

    private HeadsetSettingsWindow? _window;

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

        if (!_ent.TryGetComponent<EncryptionKeyHolderComponent>(Owner, out var keys))
            return;

        _window.UpdateState(component, keys, _proto);
    }

}
