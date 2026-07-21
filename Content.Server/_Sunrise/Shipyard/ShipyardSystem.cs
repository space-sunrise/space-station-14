using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.Shipyard;
using Content.Shared._Sunrise.Shipyard.Components;
using Content.Shared._Sunrise.Shipyard.Events;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Shipyard;

/// <summary>
/// Handles shipyard console transactions and shuttle ownership.
/// </summary>
public sealed partial class ShipyardSystem : EntitySystem
{
    private const string InvalidVesselMessage = "shipyard-console-invalid-vessel";
    private static readonly TimeSpan PurchaseDeploymentDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan TransactionDelay = TimeSpan.FromSeconds(30);

    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly DockingSystem _docking = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
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
    private readonly List<PendingShipyardPurchase> _pendingPurchases = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ShipyardConsoleComponent>(ShipyardConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<ShipyardConsolePurchaseMessage>(OnPurchase);
            subs.Event<ShipyardConsoleSellMessage>(OnSell);
        });

        SubscribeLocalEvent<ShipyardConsoleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<StationBankAccountComponent, BankBalanceUpdatedEvent>(OnBankBalanceUpdated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePendingPurchases();

        for (var i = _pendingActions.Count - 1; i >= 0; i--)
        {
            var action = _pendingActions[i];
            if (_timing.CurTime < action.CompleteAt)
                continue;

            _pendingActions.RemoveAt(i);
            if (!TryComp<ShipyardConsoleComponent>(action.Console, out var console))
                continue;

            CompleteSale((action.Console, console), action);
        }
    }

    private void OnConsoleShutdown(Entity<ShipyardConsoleComponent> ent, ref ComponentShutdown args)
    {
        for (var i = _pendingPurchases.Count - 1; i >= 0; i--)
        {
            var purchase = _pendingPurchases[i];
            if (purchase.Console != ent.Owner)
                continue;

            _pendingPurchases.RemoveAt(i);
            RefundPendingPurchase(purchase);
            DeleteStagingMap(purchase.StagingMap);
        }

        for (var i = _pendingActions.Count - 1; i >= 0; i--)
        {
            if (_pendingActions[i].Console == ent.Owner)
                _pendingActions.RemoveAt(i);
        }
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

    private void Deny(
        Entity<ShipyardConsoleComponent> ent,
        EntityUid user,
        string message,
        params (string Key, object Value)[] args)
    {
        _audio.PlayPvs(ent.Comp.ErrorSound, ent);
        if (Exists(user))
            _popup.PopupEntity(Loc.GetString(message, args), ent, user);
        UpdateUi(ent);
    }

    private void Announce(
        Entity<ShipyardConsoleComponent> ent,
        string message,
        params (string Key, object Value)[] args)
    {
        _radio.SendRadioMessage(ent, Loc.GetString(message, args), ent.Comp.AnnouncementChannel, ent, escapeMarkup: false);
    }
}
