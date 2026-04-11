using Content.Client.Administration.Managers;
using Robust.Client.GameObjects;
using Content.Client.UserInterface.Systems.Sandbox;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Sandbox;

/// <summary>
/// Manages the mapping access overlay and exposes its current UI-facing state.
/// </summary>
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

    /// <summary>
    /// Raised after the overlay state or filter settings change.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Indicates whether the overlay is currently active.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>
    /// Indicates whether the current user may enable the overlay.
    /// </summary>
    public bool CanEnable => _admin.HasFlag(AdminFlags.Mapping);

    /// <summary>
    /// Gets the active body filter used by the overlay.
    /// </summary>
    public MappingAccessBodyFilter BodyFilter { get; private set; } = MappingAccessBodyFilter.Both;

    /// <summary>
    /// Indicates whether only contained electronics should provide displayed access.
    /// </summary>
    public bool ElectronicsOnly { get; private set; }

    /// <summary>
    /// Creates the overlay instance and starts tracking admin permission changes.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _admin.AdminStatusUpdated += OnAdminStatusUpdated;
        _overlay = new(EntityManager, _entityLookup, _spriteSystem, _prototypeManager, Loc, _resourceCache, _uiManager);
        _overlay.MarkAccessReaderLookupDirty();
        _overlay.BodyFilter = BodyFilter;
        _overlay.ElectronicsOnly = ElectronicsOnly;

        SubscribeLocalEvent<AccessReaderComponent, ComponentStartup>(OnAccessReaderStartup);
        SubscribeLocalEvent<AccessReaderComponent, ComponentShutdown>(OnAccessReaderShutdown);
        SubscribeLocalEvent<AccessReaderComponent, ComponentRemove>(OnAccessReaderRemove);
        SubscribeLocalEvent<AccessReaderComponent, AccessReaderConfigurationChangedEvent>(OnAccessReaderChanged);
        UpdateUi();
    }

    /// <summary>
    /// Removes the overlay and detaches admin-status listeners.
    /// </summary>
    public override void Shutdown()
    {
        if (_overlayManager.HasOverlay<MappingAccessOverlay>())
            _overlayManager.RemoveOverlay(_overlay);

        _overlay.Dispose();

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

    /// <summary>
    /// Attempts to enable or disable the overlay.
    /// </summary>
    public bool TrySetEnabled(bool enabled)
    {
        if (Enabled == enabled)
            return true;

        if (!CanSetEnabled(enabled))
            return false;

        SetEnabled(enabled);
        return true;
    }

    /// <summary>
    /// Returns whether the requested enabled state is currently allowed.
    /// </summary>
    public bool CanSetEnabled(bool enabled)
    {
        return !enabled || CanEnable;
    }

    /// <summary>
    /// Updates which body types receive access labels.
    /// </summary>
    public void SetBodyFilter(MappingAccessBodyFilter filter)
    {
        if (BodyFilter == filter)
            return;

        BodyFilter = filter;
        _overlay.BodyFilter = filter;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Toggles whether displayed access should come only from contained electronics.
    /// </summary>
    public void SetElectronicsOnly(bool electronicsOnly)
    {
        if (ElectronicsOnly == electronicsOnly)
            return;

        ElectronicsOnly = electronicsOnly;
        _overlay.ElectronicsOnly = electronicsOnly;

        if (electronicsOnly)
            _overlay.MarkAccessReaderLookupDirty();

        StateChanged?.Invoke();
    }

    private void OnAccessReaderStartup(Entity<AccessReaderComponent> ent, ref ComponentStartup args)
    {
        _overlay.SyncAccessReaderLookup(ent.Owner, ent.Comp);
    }

    private void OnAccessReaderShutdown(Entity<AccessReaderComponent> ent, ref ComponentShutdown args)
    {
        _overlay.RemoveAccessReaderLookup(ent.Owner);
    }

    private void OnAccessReaderRemove(Entity<AccessReaderComponent> ent, ref ComponentRemove args)
    {
        _overlay.RemoveAccessReaderLookup(ent.Owner);
    }

    private void OnAccessReaderChanged(Entity<AccessReaderComponent> ent, ref AccessReaderConfigurationChangedEvent args)
    {
        _overlay.SyncAccessReaderLookup(ent.Owner, ent.Comp);
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
