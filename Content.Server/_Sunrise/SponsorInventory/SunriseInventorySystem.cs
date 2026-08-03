using System.Collections.Generic;
using Content.Server._Sunrise.PlayerCache;
using Content.Server._Sunrise.SponsorValidation;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Storage.EntitySystems;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.SponsorInventory;

/// <summary>
/// Authoritative server-side application and persistence flow for sponsor inventory profile data.
/// </summary>
public sealed partial class SunriseInventorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly StationSpawningSystem _spawn = default!;
    [Dependency] private readonly PlayerCacheManager _playerCache = default!;
    [Dependency] private readonly SponsorValidationSystem _validation = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const int MaxInventoryPurchaseIdLength = 128;
    private const int MaxPetSelectionIdLength = 128;
    private static readonly TimeSpan InitialDataRequestCooldown = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PurchaseRequestTimeout = TimeSpan.FromSeconds(15);

    private readonly Dictionary<NetUserId, Dictionary<int, SunriseInventoryProfile>> _profiles = new();
    private readonly Dictionary<NetUserId, TimeSpan> _nextInitialDataRequests = new();
    private ISharedSponsorsManager? _sponsors;

    public override void Initialize()
    {
        base.Initialize();

        IoCManager.Instance!.TryResolveType(out _sponsors);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeNetworkEvent<SunriseInventoryInitialDataRequestEvent>(OnInitialDataRequest);
        SubscribeNetworkEvent<SunriseInventoryPetSelectedEvent>(OnPetSelected);
        SubscribeNetworkEvent<SunriseInventoryProfileChangedEvent>(OnInventoryProfileChanged);
        SubscribeNetworkEvent<SunriseInventoryPurchaseRequestEvent>(OnPurchaseRequest);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        _sponsors?.LoadedSponsorInventoryInitialData += OnSponsorInventoryInitialDataLoaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
        _sponsors?.LoadedSponsorInventoryInitialData -= OnSponsorInventoryInitialDataLoaded;
        _profiles.Clear();
        _nextInitialDataRequests.Clear();
    }
}
