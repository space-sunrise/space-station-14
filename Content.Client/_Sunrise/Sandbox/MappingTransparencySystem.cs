using Content.Client.Administration.Managers;
using Content.Client.UserInterface.Systems.Sandbox;
using Content.Shared.Administration;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Sandbox;

public sealed class MappingTransparencySystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private MappingTransparencyOverlay _overlay = default!;

    public bool Enabled { get; private set; }
    public bool CanEnable => _admin.HasFlag(AdminFlags.Mapping);

    public override void Initialize()
    {
        base.Initialize();

        _admin.AdminStatusUpdated += OnAdminStatusUpdated;
        _overlay = new();
        UpdateUi();
    }

    public override void Shutdown()
    {
        if (_overlayMan.HasOverlay<MappingTransparencyOverlay>())
        {
            _overlay.ResetTransparency();
            _overlayMan.RemoveOverlay(_overlay);
        }

        base.Shutdown();

        _admin.AdminStatusUpdated -= OnAdminStatusUpdated;
    }

    private void OnAdminStatusUpdated()
    {
        if (Enabled && !CanSetEnabled(true))
            SetEnabled(false);
        else
            UpdateUi();
    }

    public bool TrySetEnabled(bool enabled)
    {
        if (Enabled == enabled)
            return true;

        if (!CanSetEnabled(enabled))
            return false;

        SetEnabled(enabled);
        return true;
    }

    public bool CanSetEnabled(bool enabled)
    {
        return !enabled || CanEnable;
    }

    private void SetEnabled(bool enabled)
    {
        Enabled = enabled;

        if (enabled)
        {
            if (!_overlayMan.HasOverlay<MappingTransparencyOverlay>())
                _overlayMan.AddOverlay(_overlay);
        }
        else
        {
            _overlay.ResetTransparency();
            _overlayMan.RemoveOverlay(_overlay);
        }

        UpdateUi();
    }

    private void UpdateUi()
    {
        var controller = _ui.GetUIController<SandboxUIController>();
        controller.SetMappingTransparencyVisible(CanEnable);
        controller.SetToggleMappingTransparency(Enabled);
    }
}
