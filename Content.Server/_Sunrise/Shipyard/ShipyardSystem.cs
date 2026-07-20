using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Server.Cargo.Systems;
using Content.Shared._Sunrise.Shipyard;
using Content.Shared._Sunrise.Shipyard.BUI;
using Content.Shared._Sunrise.Shipyard.Components;
using Content.Shared._Sunrise.Shipyard.Events;
using Content.Shared._Sunrise.Shipyard.Prototypes;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.SSDIndicator;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Shipyard;

public sealed class ShipyardSystem : EntitySystem
{
    private const string InvalidVesselMessage = "shipyard-console-invalid-vessel";
    private static readonly TimeSpan TransactionDelay = TimeSpan.FromSeconds(30);

    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly DockingSystem _docking = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly List<PendingShipyardAction> _pendingActions = new();

    public override void Initialize()
    {
        Subs.BuiEvents<ShipyardConsoleComponent>(ShipyardConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<ShipyardConsolePurchaseMessage>(OnPurchase);
            subs.Event<ShipyardConsoleSellMessage>(OnSell);
        });

        SubscribeLocalEvent<ShipyardConsoleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        for (var i = _pendingActions.Count - 1; i >= 0; i--)
        {
            var action = _pendingActions[i];
            if (_timing.CurTime < action.CompleteAt)
                continue;

            _pendingActions.RemoveAt(i);
            if (!TryComp<ShipyardConsoleComponent>(action.Console, out var console))
                continue;

            var ent = (action.Console, console);
            CompleteSale(ent, action);
        }
    }

    private void OnConsoleShutdown(Entity<ShipyardConsoleComponent> ent, ref ComponentShutdown args)
    {
        for (var i = _pendingActions.Count - 1; i >= 0; i--)
        {
            if (_pendingActions[i].Console == ent.Owner)
                _pendingActions.RemoveAt(i);
        }
    }

    private void OnUiOpened(Entity<ShipyardConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnInteractUsing(Entity<ShipyardConsoleComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<CashComponent>(args.Used))
            return;

        if (!_access.IsAllowed(args.User, ent))
        {
            Deny(ent, args.User, "shipyard-console-access-denied");
            return;
        }

        var amount = (int) _pricing.GetPrice(args.Used);
        if (amount <= 0)
            return;

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
        {
            Deny(ent, args.User, "shipyard-console-station-not-found");
            return;
        }

        if (!_cargo.TryAdjustBankAccount((station, bank), ent.Comp.Account, amount))
        {
            Deny(ent, args.User, "shipyard-console-account-not-found");
            return;
        }

        QueueDel(args.Used);
        args.Handled = true;
        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        _popup.PopupEntity(Loc.GetString("shipyard-console-credit-deposit", ("amount", amount)), ent, args.User);
        UpdateUi(ent);
    }

    private void OnPurchase(Entity<ShipyardConsoleComponent> ent, ref ShipyardConsolePurchaseMessage args)
    {
        if (!_access.IsAllowed(args.Actor, ent))
        {
            Deny(ent, args.Actor, "shipyard-console-access-denied");
            return;
        }

        if (HasPendingAction(ent.Owner))
        {
            Deny(ent, args.Actor, "shipyard-console-action-pending");
            return;
        }

        if (ent.Comp.CurrentShuttle is { } current && Exists(current))
        {
            Deny(ent, args.Actor, "shipyard-console-sell-first");
            return;
        }

        if (!TryGetVessel(ent.Comp, args.VesselId, out var vessel))
        {
            Deny(ent, args.Actor, InvalidVesselMessage);
            return;
        }

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
        {
            Deny(ent, args.Actor, "shipyard-console-station-not-found");
            return;
        }

        if (!_cargo.TryGetAccount((station, bank), ent.Comp.Account, out var balance))
        {
            Deny(ent, args.Actor, "shipyard-console-account-not-found");
            return;
        }

        if (vessel.Price < 0 || balance < vessel.Price)
        {
            Deny(ent, args.Actor, "shipyard-console-insufficient-funds", ("cost", vessel.Price));
            return;
        }

        var mapId = Transform(ent).MapID;
        var coordinates = _transform.GetWorldPosition(ent) + vessel.SpawnOffset;
        if (!_mapLoader.TryLoadGrid(mapId, vessel.GridPath, out var shuttleGrid, offset: coordinates, rot: vessel.Rotation))
        {
            Deny(ent, args.Actor, "shipyard-console-load-failed");
            return;
        }

        if (!_cargo.TryAdjustBankAccount((station, bank), ent.Comp.Account, -vessel.Price))
        {
            QueueDel(shuttleGrid.Value.Owner);
            Deny(ent, args.Actor, "shipyard-console-account-not-found");
            return;
        }

        var shuttleUid = shuttleGrid.Value.Owner;
        ent.Comp.CurrentShuttle = shuttleUid;
        ent.Comp.CurrentShuttlePrice = vessel.Price;
        ent.Comp.CurrentShuttleName = Loc.GetString(vessel.Name);
        Dirty(ent);

        TryDockPurchasedShuttle(shuttleUid, station, ent.Comp.PriorityTag);
        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        _popup.PopupEntity(Loc.GetString("shipyard-console-purchase-success"), ent, args.Actor);
        Announce(ent, "shipyard-console-purchase-announcement",
            ("ship", Loc.GetString(vessel.Name)), ("cost", vessel.Price));
        UpdateUi(ent);
    }

    private void OnSell(Entity<ShipyardConsoleComponent> ent, ref ShipyardConsoleSellMessage args)
    {
        if (!_access.IsAllowed(args.Actor, ent))
        {
            Deny(ent, args.Actor, "shipyard-console-access-denied");
            return;
        }

        if (HasPendingAction(ent.Owner))
        {
            Deny(ent, args.Actor, "shipyard-console-action-pending");
            return;
        }

        if (ent.Comp.CurrentShuttle is not { } shuttleUid || !Exists(shuttleUid))
        {
            ClearShuttle(ent);
            Deny(ent, args.Actor, "shipyard-console-no-shuttle");
            return;
        }

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
        {
            Deny(ent, args.Actor, "shipyard-console-station-not-found");
            return;
        }

        if (!IsShuttleNearStation(shuttleUid, station, ent.Comp.MaxSellDistance))
        {
            Deny(ent, args.Actor, "shipyard-console-shuttle-too-far", ("distance", ent.Comp.MaxSellDistance));
            return;
        }

        var occupantCount = GetShuttleOccupantCount(shuttleUid);
        if (occupantCount > 0)
        {
            Deny(ent, args.Actor, "shipyard-console-shuttle-occupied", ("count", occupantCount));
            return;
        }

        var refund = (int) MathF.Round(ent.Comp.CurrentShuttlePrice * Math.Clamp(ent.Comp.SellRate, 0f, 1f));
        if (!_cargo.TryGetAccount((station, bank), ent.Comp.Account, out _))
        {
            Deny(ent, args.Actor, "shipyard-console-account-not-found");
            return;
        }

        var soldName = ent.Comp.CurrentShuttleName ?? Loc.GetString("shipyard-console-unknown-shuttle");
        _pendingActions.Add(new PendingShipyardAction(
            ent.Owner,
            args.Actor,
            _timing.CurTime + TransactionDelay,
            soldName,
            refund));
        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        _popup.PopupEntity(Loc.GetString("shipyard-console-sale-queued",
            ("delay", TransactionDelay.TotalSeconds), ("ship", soldName)), ent, args.Actor);
        Announce(ent, "shipyard-console-sale-queued-announcement",
            ("ship", soldName), ("delay", TransactionDelay.TotalSeconds));
        UpdateUi(ent);
    }

    private void CompleteSale(Entity<ShipyardConsoleComponent> ent, PendingShipyardAction action)
    {
        if (ent.Comp.CurrentShuttle is not { } shuttleUid || !Exists(shuttleUid))
        {
            ClearShuttle(ent);
            CancelAction(ent, action, "shipyard-console-no-shuttle");
            return;
        }

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bank))
        {
            CancelAction(ent, action, "shipyard-console-station-not-found");
            return;
        }

        if (!IsShuttleNearStation(shuttleUid, station, ent.Comp.MaxSellDistance))
        {
            CancelAction(ent, action, "shipyard-console-shuttle-too-far", ("distance", ent.Comp.MaxSellDistance));
            return;
        }

        var occupantCount = GetShuttleOccupantCount(shuttleUid);
        if (occupantCount > 0)
        {
            CancelAction(ent, action, "shipyard-console-shuttle-occupied", ("count", occupantCount));
            return;
        }

        if (!_cargo.TryAdjustBankAccount((station, bank), ent.Comp.Account, action.Amount))
        {
            CancelAction(ent, action, "shipyard-console-account-not-found");
            return;
        }

        QueueDel(shuttleUid);
        ClearShuttle(ent);
        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        PopupActionActor(ent, action, "shipyard-console-sell-success");
        Announce(ent, "shipyard-console-sale-announcement", ("ship", action.ShipName!), ("refund", action.Amount));
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ShipyardConsoleComponent> ent)
    {
        var accountName = Loc.GetString("shipyard-console-no-account");
        var accountColor = Color.White;
        var balance = 0;
        if (_prototype.TryIndex<CargoAccountPrototype>(ent.Comp.Account, out var account))
        {
            accountName = Loc.GetString(account.Name);
            accountColor = account.Color;
        }

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid is { } station &&
            TryComp<StationBankAccountComponent>(station, out var bank))
        {
            balance = _cargo.GetBalanceFromAccount((station, bank), ent.Comp.Account);
        }

        var currentSellValue = (int) MathF.Round(ent.Comp.CurrentShuttlePrice * Math.Clamp(ent.Comp.SellRate, 0f, 1f));
        if (ent.Comp.CurrentShuttle is not { } shuttle || !Exists(shuttle))
        {
            if (ent.Comp.CurrentShuttle is not null)
                ClearShuttle(ent);

            currentSellValue = 0;
        }

        var vessels = new List<ShipyardVesselData>();
        var added = new HashSet<string>();
        foreach (var vessel in _prototype.EnumeratePrototypes<ShipyardVesselPrototype>())
        {
            if (vessel.Group != ent.Comp.VesselGroup || !added.Add(vessel.ID))
                continue;

            vessels.Add(new ShipyardVesselData(
                vessel.ID,
                Loc.GetString(vessel.Name),
                Loc.GetString(vessel.Description),
                vessel.Price));
        }

        foreach (var vesselId in ent.Comp.Vessels)
        {
            if (!_prototype.TryIndex(vesselId, out ShipyardVesselPrototype? vessel) || !added.Add(vessel.ID))
                continue;

            vessels.Add(new ShipyardVesselData(
                vessel.ID,
                Loc.GetString(vessel.Name),
                Loc.GetString(vessel.Description),
                vessel.Price));
        }

        vessels.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase));

        _ui.SetUiState(ent.Owner, ShipyardConsoleUiKey.Key, new ShipyardConsoleInterfaceState(
            accountName,
            accountColor,
            balance,
            ent.Comp.CurrentShuttleName,
            ent.Comp.CurrentShuttlePrice,
            currentSellValue,
            Math.Clamp(ent.Comp.SellRate, 0f, 1f),
            HasPendingAction(ent.Owner),
            vessels));
    }

    private bool TryGetVessel(ShipyardConsoleComponent component, string vesselId, out ShipyardVesselPrototype vessel)
    {
        vessel = default!;
        if (!_prototype.TryIndex<ShipyardVesselPrototype>(vesselId, out var candidate))
            return false;

        if (candidate.Group != component.VesselGroup && !component.Vessels.Contains(candidate.ID))
            return false;

        vessel = candidate;
        return true;
    }

    private bool TryDockPurchasedShuttle(EntityUid shuttleUid, EntityUid stationUid, string? priorityTag)
    {
        if (!TryComp<ShuttleComponent>(shuttleUid, out var shuttle) ||
            !TryComp<StationDataComponent>(stationUid, out var stationData))
            return false;

        var bestConfig = GetBestDockingConfig(shuttleUid, stationData, priorityTag);
        if (bestConfig == null && !string.IsNullOrEmpty(priorityTag))
            bestConfig = GetBestDockingConfig(shuttleUid, stationData, null);

        if (bestConfig != null)
        {
            _shuttle.FTLDock((shuttleUid, Transform(shuttleUid)), bestConfig);
            return true;
        }

        var fallbackGrid = _station.GetLargestGrid((stationUid, stationData));
        if (fallbackGrid is not { } grid)
            return false;

        _shuttle.TryFTLDock(shuttleUid, shuttle, grid);
        return false;
    }

    private DockingConfig? GetBestDockingConfig(EntityUid shuttleUid, StationDataComponent stationData,
        string? priorityTag)
    {
        DockingConfig? bestConfig = null;
        foreach (var gridUid in stationData.Grids)
        {
            if (gridUid == shuttleUid || !Exists(gridUid))
                continue;

            var config = _docking.GetDockingConfig(shuttleUid, gridUid, priorityTag);
            if (config == null)
                continue;

            if (bestConfig == null || config.Docks.Count > bestConfig.Docks.Count)
                bestConfig = config;
        }

        return bestConfig;
    }

    private bool HasPendingAction(EntityUid console)
    {
        foreach (var action in _pendingActions)
        {
            if (action.Console == console)
                return true;
        }

        return false;
    }

    private void CancelAction(Entity<ShipyardConsoleComponent> ent, PendingShipyardAction action,
        string message, params (string Key, object Value)[] args)
    {
        _audio.PlayPvs(ent.Comp.ErrorSound, ent);
        PopupActionActor(ent, action, message, args);
        Announce(ent, "shipyard-console-sale-cancelled-announcement",
            ("ship", action.ShipName ?? Loc.GetString("shipyard-console-unknown-shuttle")),
            ("reason", Loc.GetString(message, args)));
        UpdateUi(ent);
    }

    private void PopupActionActor(Entity<ShipyardConsoleComponent> ent, PendingShipyardAction action,
        string message, params (string Key, object Value)[] args)
    {
        if (Exists(action.Actor))
            _popup.PopupEntity(Loc.GetString(message, args), ent, action.Actor);
    }

    private bool IsShuttleNearStation(EntityUid shuttleUid, EntityUid stationUid, float maxDistance)
    {
        if (!TryComp<StationDataComponent>(stationUid, out var stationData))
            return false;

        var shuttleTransform = Transform(shuttleUid);
        var shuttlePosition = _transform.GetWorldPosition(shuttleUid);
        var maxDistanceSquared = maxDistance * maxDistance;
        foreach (var gridUid in stationData.Grids)
        {
            if (gridUid == shuttleUid || !Exists(gridUid))
                continue;

            var gridTransform = Transform(gridUid);
            if (gridTransform.MapID != shuttleTransform.MapID)
                continue;

            if ((_transform.GetWorldPosition(gridUid) - shuttlePosition).LengthSquared() <= maxDistanceSquared)
                return true;
        }

        return false;
    }

    private int GetShuttleOccupantCount(EntityUid shuttleUid)
    {
        var occupants = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (IsEntityOnShuttle(uid, shuttleUid, xform))
                occupants.Add(uid);
        }

        var ssdQuery = EntityQueryEnumerator<SSDIndicatorComponent, MindContainerComponent, TransformComponent>();
        while (ssdQuery.MoveNext(out var uid, out var ssd, out var mind, out var xform))
        {
            var hasMind = mind.HasMind;
            if (!hasMind && mind.LastMindStored is { } lastMind)
                hasMind = Exists(lastMind);

            if (!ssd.IsSSD || !hasMind)
                continue;

            if (IsEntityOnShuttle(uid, shuttleUid, xform))
                occupants.Add(uid);
        }

        return occupants.Count;
    }

    private bool IsEntityOnShuttle(EntityUid uid, EntityUid shuttleUid, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform, false))
            return false;

        if (xform.GridUid == shuttleUid || xform.ParentUid == shuttleUid)
            return true;

        var parent = xform.ParentUid;
        while (parent.IsValid())
        {
            if (parent == shuttleUid)
                return true;

            if (!TryComp<TransformComponent>(parent, out var parentXform))
                return false;

            if (parentXform.GridUid == shuttleUid)
                return true;

            parent = parentXform.ParentUid;
        }

        return false;
    }

    private void ClearShuttle(Entity<ShipyardConsoleComponent> ent)
    {
        ent.Comp.CurrentShuttle = null;
        ent.Comp.CurrentShuttlePrice = 0;
        ent.Comp.CurrentShuttleName = null;
        Dirty(ent);
    }

    private void Deny(Entity<ShipyardConsoleComponent> ent, EntityUid user, string message, params (string Key, object Value)[] args)
    {
        _audio.PlayPvs(ent.Comp.ErrorSound, ent);
        _popup.PopupEntity(Loc.GetString(message, args), ent, user);
        UpdateUi(ent);
    }

    private void Announce(Entity<ShipyardConsoleComponent> ent, string message, params (string Key, object Value)[] args)
    {
        _radio.SendRadioMessage(ent, Loc.GetString(message, args), ent.Comp.AnnouncementChannel, ent, escapeMarkup: false);
    }

    private readonly record struct PendingShipyardAction(
        EntityUid Console,
        EntityUid Actor,
        TimeSpan CompleteAt,
        string? ShipName,
        int Amount);
}
