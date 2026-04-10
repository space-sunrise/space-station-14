using System;
using Content.Client.Administration.Managers;
using Robust.Client.GameObjects;
using Content.Client.UserInterface.Systems.Sandbox;
using Content.Shared.Administration;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Sandbox;

public sealed class MappingAccessOverlaySystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private MappingAccessOverlay _overlay = default!;

    public event Action? StateChanged;

    public bool Enabled { get; private set; }
    public bool CanEnable => _admin.HasFlag(AdminFlags.Mapping);
    public MappingAccessBodyFilter BodyFilter { get; private set; } = MappingAccessBodyFilter.Both;

    public override void Initialize()
    {
        base.Initialize();

        _admin.AdminStatusUpdated += OnAdminStatusUpdated;
        _overlay = new(EntityManager, _entityLookup, _spriteSystem, _prototypeManager, Loc, _resourceCache, _uiManager);
        _overlay.BodyFilter = BodyFilter;
        UpdateUi();
    }

    public override void Shutdown()
    {
        if (_overlayManager.HasOverlay<MappingAccessOverlay>())
            _overlayManager.RemoveOverlay(_overlay);

        base.Shutdown();

        _admin.AdminStatusUpdated -= OnAdminStatusUpdated;
    }

    private void OnAdminStatusUpdated()
    {
        if (Enabled && !CanSetEnabled(true))
            SetEnabled(false);
        else
        {
            UpdateUi();
            StateChanged?.Invoke();
        }
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

    public void SetBodyFilter(MappingAccessBodyFilter filter)
    {
        if (BodyFilter == filter)
            return;

        BodyFilter = filter;
        _overlay.BodyFilter = filter;
        StateChanged?.Invoke();
    }

    private void SetEnabled(bool enabled)
    {
        Enabled = enabled;

        if (enabled)
        {
            if (!_overlayManager.HasOverlay<MappingAccessOverlay>())
                _overlayManager.AddOverlay(_overlay);
        }
        else
        {
            _overlayManager.RemoveOverlay(_overlay);
        }

        UpdateUi();
        StateChanged?.Invoke();
    }

    private void UpdateUi()
    {
        var controller = _uiManager.GetUIController<SandboxUIController>();
        controller.SetMappingAccessVisible(CanEnable);
        controller.SetToggleMappingAccess(Enabled);
    }
}
