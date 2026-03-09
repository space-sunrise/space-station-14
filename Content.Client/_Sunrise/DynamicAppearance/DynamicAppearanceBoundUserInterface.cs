using Content.Client.Administration.Managers;
using Content.Shared._Sunrise.DynamicAppearance;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.DynamicAppearance;

public sealed class DynamicAppearanceBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ViewVariables]
    private DynamicAppearanceWindow? _window;

    private DynamicAppearanceBUIState? _lastState;
    private DynamicAppearancePermissionsMessage? _lastPermissions;

    public DynamicAppearanceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        if (IsOpened || _window != null)
            return;

        if (!CanOpenLocally())
            return;

        base.Open();

        _window = this.CreateWindow<DynamicAppearanceWindow>();

        _window.OnSave += () =>
        {
            if (_window == null)
                return;

            SendMessage(new DynamicAppearanceSaveMessage(_window.DraftState));
        };

        _window.OnReset += () =>
        {
            if (_lastState != null)
                _window?.UpdateState(_lastState);
        };

        _window.OnAdminOverrideChanged += enabled =>
        {
            SendMessage(new DynamicAppearanceSetAdminOverrideMessage(enabled));
        };

        // If the server already pushed a state before Open(), apply it now.
        if (_lastState != null)
            _window.UpdateState(_lastState);

        if (_lastPermissions != null)
            _window.UpdatePermissions(_lastPermissions);
    }

    protected override void Dispose(bool disposing)
    {
        _window = null;
        base.Dispose(disposing);
    }

    private bool CanOpenLocally()
    {
        if (_adminManager.IsAdmin())
            return true;

        if (_playerManager.LocalEntity is not { } localPlayer)
            return false;

        if (localPlayer != Owner)
            return false;

        return EntMan.TryGetComponent(Owner, out DynamicAppearanceComponent? component)
               && component.AllowedFields != DynamicAppearanceFields.None;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DynamicAppearanceBUIState data)
            return;

        _lastState = data;
        _window?.UpdateState(data);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is not DynamicAppearancePermissionsMessage permissions)
            return;

        _lastPermissions = permissions;
        _window?.UpdatePermissions(permissions);
    }
}
