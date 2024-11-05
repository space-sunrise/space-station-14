using Content.Server._Sunrise.StationCentComm;
using Content.Server.Access.Systems;
using Content.Server.AlertLevel;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Content.Shared.Access.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.CentCom;

public sealed class CentComConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CentComConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CentComConsoleComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CentComConsoleComponent, UnanchorAttemptEvent>(OnUnanchor);
        SubscribeLocalEvent<CentComConsoleComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<CentComConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUi);
        SubscribeLocalEvent<CentComConsoleComponent, EntRemovedFromContainerMessage>(UpdateUi);
        SubscribeLocalEvent<CentComConsoleComponent, BoundUIOpenedEvent>(UpdateUi);
        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<CentComConsoleComponent, CentComConsoleCallEmergencyShuttleMessage>(OnCall);
        SubscribeLocalEvent<CentComConsoleComponent, CentComConsoleRecallEmergencyShuttleMessage>(OnRecall);
        SubscribeLocalEvent<CentComConsoleComponent, CentComConsoleAnnounceMessage>(OnAnnounce);
        SubscribeLocalEvent<CentComConsoleComponent, CentComConsoleAlertLevelChangeMessage>(OnAlert);
    }

    private void OnCall(EntityUid uid,
        CentComConsoleComponent component,
        CentComConsoleCallEmergencyShuttleMessage args)
    {
        if (!(component.IdSlot.Item.HasValue && CheckPermissions(uid, component.IdSlot.Item.Value)))
            return;
        _roundEndSystem.RequestRoundEnd(args.Actor);
    }

    private void OnRecall(EntityUid uid,
        CentComConsoleComponent component,
        CentComConsoleRecallEmergencyShuttleMessage args)
    {
        if (!(component.IdSlot.Item.HasValue && CheckPermissions(uid, component.IdSlot.Item.Value)))
            return;
    }

    private void OnAnnounce(EntityUid uid, CentComConsoleComponent component, CentComConsoleAnnounceMessage args)
    {
        if (!(component.IdSlot.Item.HasValue && CheckPermissions(uid, component.IdSlot.Item.Value)))
            return;
    }

    private void OnAlert(EntityUid uid, CentComConsoleComponent component, CentComConsoleAlertLevelChangeMessage args)
    {
        if (!(component.IdSlot.Item.HasValue && CheckPermissions(uid, component.IdSlot.Item.Value)))
            return;
    }

    private void OnRoundEnd(RoundEndSystemChangedEvent args)
    {
        var query = EntityQueryEnumerator<CentComConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUi(uid, comp, args);
        }
    }

    private void OnComponentInit(EntityUid uid, CentComConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, CentComConsoleComponent.IdCardSlotId, component.IdSlot);
    }

    private void OnComponentRemove(EntityUid uid, CentComConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.IdSlot);
    }

    private void UpdateStation(EntityUid uid, CentComConsoleComponent component)
    {
        var uUid = _station.GetStationInMap(_transform.GetMapId(uid));
        if (uUid == null)
            return;
        if (!TryComp<CentCommStationComponent>(uUid, out var centCommStationComponent))
            return;

        var stationUid = centCommStationComponent.ParentStation;

        var meta = MetaData(stationUid);
        if (!TryComp<AlertLevelComponent>(stationUid, out var alertLevelComponent))
            return;
        List<string> alertLevels = [];
        if (alertLevelComponent.AlertLevels != null)
        {
            foreach (var i in alertLevelComponent.AlertLevels.Levels)
            {
                alertLevels.Add(i.Key);
            }
        }
        component.Station = new LinkedStation()
        {
            Name = meta.EntityName,
            AlertLevels = alertLevels,
            CurrentAlert = alertLevelComponent.CurrentLevel,
            DefaultDelay = TimeSpan.FromMinutes(10),
        };
    }

    private void OnAnchorStateChanged(EntityUid uid, CentComConsoleComponent component, AnchorStateChangedEvent args)
    {
        if (args.Anchored == true)
        {
            UpdateStation(uid, component);
        }
    }

    private void OnUnanchor(EntityUid uid, CentComConsoleComponent component, UnanchorAttemptEvent args)
    {
        args.Cancel();
    }

    private void UpdateUi(EntityUid uid, CentComConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;
        UpdateStation(uid, component);

        var idName = string.Empty;
        var idPresent = component.IdSlot.Item.HasValue;
        var idEnoughPermissions = component.IdSlot.Item.HasValue && CheckPermissions(uid, component.IdSlot.Item.Value);

        if (idPresent && component.IdSlot.Item != null)
        {
            idName = MetaData(component.IdSlot.Item.Value).EntityName;
        }

        var sentEvac = _roundEndSystem.ShuttleTimeLeft != null;
        var dockTime = _roundEndSystem.ShuttleTimeLeft;

        var newState = new CentComConsoleBoundUserInterfaceState(
            idPresent,
            idEnoughPermissions,
            idName,
            component.Station,
            GetNetEntity(uid),
            sentEvac,
            dockTime);

        _userInterface.SetUiState(uid, CentComConsoleUiKey.Key, newState);
    }

    private bool CheckPermissions(EntityUid target, EntityUid idCard)
    {
        return _accessReader.IsAllowed(idCard, target);
    }
}
